﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TerrainPainter;




[ExecuteInEditMode]
[RequireComponent(typeof(Terrain))]
public class TerrainPainter_Terrain : MonoBehaviour
{

    [HideInInspector]
    public bool hasTerrainHeigtmapChanged = false;


    public Terrain terrain;
    public ComputeShader computeShader;
    public TerrainPainter_Manager manager;
    public int terrainIndex = -1;



    // genereted maps
    public RenderTexture renderTexture_unity_heightMap;
    public RenderTexture renderTexture_neighbor_terrain_heightMaps;
    public RenderTexture renderTexture_waterMap_left;
    public RenderTexture renderTexture_waterMap_up;
    public RenderTexture renderTexture_waterMap_right;
    public RenderTexture renderTexture_waterMap_down;
    public RenderTexture renderTexture_neighbor_terrain_waterMaps;
    public RenderTexture renderTexture_waterOutMap_this;
    public RenderTexture renderTexture_waterOutMap_left;
    public RenderTexture renderTexture_waterOutMap_up;
    public RenderTexture renderTexture_waterOutMap_right;
    public RenderTexture renderTexture_waterOutMap_down;
    public RenderTexture renderTexture_height_slope_snowWeight_Maps;
    public RenderTexture renderTexture_convexity_concavitiy_flow_Maps;





    // other paaramters
    public int extraThread = 8;
    public int hm_x;  // heightMap_Width
    public int hm_y;  // heightMap_Height
    public Vector4 terrainSize;
    public Vector4 terrainPosition;
    public int terrainHeightMapResolution;
    public int alphamapResolution;
    public int am_x;            //  alphaMap_Width ;
    public int am_y;            //  alphaMap_Height
    public int bm_x;            //  baseMap_Width ;
    public int bm_y;            //  baseMap_Height

    public Vector4 hasNeighborTerrains;
    public Vector4 cornerNeighborTerrainsHeights;



    public TerrainPainter_Splat[] splats;


    // paint rules and paint buffer
    public SplatPaintRules[] splatPaintRulesArray;
    public ComputeBuffer splatPaintRulesBuffer;


    // splatMap output
    private float[] splat_Map_Total_Weight;
    public ComputeBuffer splat_Map_Total_Weight_Buffer;
    public RenderTexture[] splatMapsArray;


    public float snowAmount = 0.75f;


    public int flowMapIteration = 10;















    //  FUNCTIONS


    [ExecuteInEditMode]
    void OnTerrainChanged(TerrainChangedFlags flag)
    {
        if (flag == TerrainChangedFlags.DelayedHeightmapUpdate)
        {
            hasTerrainHeigtmapChanged = true;
            manager.TerrainHeightmapChanged(terrainIndex) ;
        }
    }




    public void SetUpProperties()
    {
        terrain = this.GetComponent<Terrain>();
        computeShader = manager.computeShader;
        snowAmount = manager.snowAmount;
        flowMapIteration = manager.flowMapIteration;

        SetUpTerrainParameters();
        SetUpTerrainLayers();
        SetUpSplatPaintRulesArray();
        SetUpTextures();
    }


/*
    public void UpdateProperties()
    {
        renderTexture_unity_heightMap.Create();

        renderTexture_height_slope_snowWeight_Maps.Create();

        renderTexture_convexity_concavitiy_flow_Maps.Create();

        renderTexture_neighbor_terrain_heightMaps.Create();


        splatMapsArray = new RenderTexture[Mathf.CeilToInt(((float)terrain.terrainData.terrainLayers.Length) / 4f)];
        for (int i = 0; i < splatMapsArray.Length; i++)
        {
            splatMapsArray[i] = new RenderTexture(am_x, am_y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            splatMapsArray[i].enableRandomWrite = true;
            splatMapsArray[i].Create();
        }
    }
*/


    void SetUpTerrainParameters()
    {
        hm_x = terrain.terrainData.heightmapWidth ;
        hm_y = terrain.terrainData.heightmapHeight ;

        am_x = terrain.terrainData.alphamapWidth ;
        am_y = terrain.terrainData.alphamapHeight ;

        bm_x = terrain.terrainData.baseMapResolution;
        bm_y = terrain.terrainData.baseMapResolution;

        terrainSize = (Vector4)(terrain.terrainData.size);
        terrainPosition = (Vector4)(this.transform.position);
        terrainHeightMapResolution = terrain.terrainData.heightmapResolution ;
        alphamapResolution = terrain.terrainData.alphamapResolution;

        hasNeighborTerrains = new Vector4
            (
            (terrain.leftNeighbor != null ? 1f : -1f),
            (terrain.topNeighbor != null ? 1f : -1f),
            (terrain.rightNeighbor != null ? 1f : -1f),
            (terrain.bottomNeighbor != null ? 1f : -1f)
            );
    }




    public void SetUpTerrainLayers()
    {
        splats = manager.splats;
        terrain.terrainData.terrainLayers = manager.terrainLayers;
    }




    void SetUpSplatPaintRulesArray()
    {

        if (splats == null || (splats != null && splats.Length == 0))
            return;
        

        splatPaintRulesArray = new SplatPaintRules[splats.Length];

        for (int i = 0; i < splats.Length; i++)
        {
            splatPaintRulesArray[i] = splats[i].paintRules;
            splatPaintRulesArray[i].flowMapWeight = splats[i].useFlowMapMask == true ? splats[i].paintRules.flowMapWeight : -1f;
            splatPaintRulesArray[i].convexityMapWeight = splats[i].useConvexityMapMask == true ? splats[i].paintRules.convexityMapWeight : -1f;
            splatPaintRulesArray[i].concavityMapWeight = splats[i].useConcavitiyMapMask == true ? splats[i].paintRules.concavityMapWeight : -1f;
        }
    }


    void SetUpSplatPaintRulesBuffer()
    {
        if (splatPaintRulesBuffer != null)
            splatPaintRulesBuffer.Release();


        splatPaintRulesBuffer = new ComputeBuffer(splatPaintRulesArray.Length, 10 * sizeof(float));

        splatPaintRulesBuffer.SetData(splatPaintRulesArray);
    }


    void SetUpSplatmapTotalWeightBuffer()
    {
          splat_Map_Total_Weight = new float[am_x * am_y];

        if (splat_Map_Total_Weight_Buffer != null)
            splat_Map_Total_Weight_Buffer.Release();

        splat_Map_Total_Weight_Buffer = new ComputeBuffer(splat_Map_Total_Weight.Length, sizeof(float));
    }


    void SetUpTextures()
    {
        renderTexture_unity_heightMap = CreateRenderTexture(hm_x, terrain.terrainData.heightmapTexture.format);
    //    renderTexture_waterMap = CreateRenderTexture(hm_x, RenderTextureFormat.R16);
    //    renderTexture_waterOutMap_this = CreateRenderTexture(hm_x);
        renderTexture_height_slope_snowWeight_Maps = CreateRenderTexture(hm_x);
    //    renderTexture_normal_Map = CreateRenderTexture(hm_x);
        renderTexture_convexity_concavitiy_flow_Maps = CreateRenderTexture(hm_x);
        renderTexture_neighbor_terrain_heightMaps = CreateRenderTexture(hm_x);

        splatMapsArray = new RenderTexture[Mathf.CeilToInt(((float)terrain.terrainData.terrainLayers.Length) / 4f)];
        for (int i = 0; i < splatMapsArray.Length; i++)
        {
            splatMapsArray[i] = new RenderTexture(am_x, am_y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            splatMapsArray[i].enableRandomWrite = true;
            splatMapsArray[i].Create();
        }
    }
















    RenderTexture CreateRenderTexture(int p_size)
    {
        RenderTexture _rT = new RenderTexture(terrain.terrainData.heightmapTexture.descriptor);
        _rT.width = p_size;
        _rT.height = p_size;
        _rT.format = RenderTextureFormat.ARGB64;
        _rT.enableRandomWrite = true;
        _rT.Create();
        return _rT;
    }

    RenderTexture CreateRenderTexture(int p_size, RenderTextureFormat p_format)
    {
        RenderTexture _rT = new RenderTexture(terrain.terrainData.heightmapTexture.descriptor);
        _rT.width = p_size;
        _rT.height = p_size;
        _rT.format = p_format;
        _rT.enableRandomWrite = true;
        _rT.Create();
        return _rT;
    }



    RenderTexture CopyRenderTexture(RenderTexture p_renderTexture)
    {
        if (p_renderTexture)
        {
            RenderTexture _rT = new RenderTexture(p_renderTexture.descriptor);
            _rT.width = p_renderTexture.width ;
            _rT.height = p_renderTexture.height ;
            _rT.enableRandomWrite = true;
            _rT.Create();
            Graphics.Blit(p_renderTexture, _rT);
            return _rT;
        }
        else
        {
            RenderTexture _rT = new RenderTexture(terrain.terrainData.heightmapTexture.descriptor);
            _rT.enableRandomWrite = true;
            _rT.Create();
            return _rT;
        }
    }


















    void AssignBuffersAndParametersFor_Generate_Height_Map_Kernel()
    {
        Graphics.Blit(terrain.terrainData.heightmapTexture, renderTexture_unity_heightMap);

        computeShader.SetTexture(NameIDs.Generate_Height_Map, NameIDs.unity_heightMap, renderTexture_unity_heightMap);
        computeShader.SetTexture(NameIDs.Generate_Height_Map, NameIDs.height_slope_snowWeight_Maps, renderTexture_height_slope_snowWeight_Maps);




        float _corner_x, _corner_y, _corner_z, _corner_w = 0f ;

        _corner_x = terrain.terrainData.GetHeight(0, hm_y);
        _corner_y = terrain.terrainData.GetHeight(hm_x, hm_y);
        _corner_z = terrain.terrainData.GetHeight(hm_x, 0);
        _corner_w = terrain.terrainData.GetHeight(0, 0);

    

        if (terrain.leftNeighbor)
        {
            if (terrain.leftNeighbor.topNeighbor)
                _corner_x = terrain.leftNeighbor.topNeighbor.terrainData.GetHeight(terrain.leftNeighbor.topNeighbor.terrainData.heightmapWidth - 1, 0);

            if (terrain.leftNeighbor.bottomNeighbor)
                _corner_w = terrain.leftNeighbor.bottomNeighbor.terrainData.GetHeight(terrain.leftNeighbor.bottomNeighbor.terrainData.heightmapWidth - 1, terrain.leftNeighbor.bottomNeighbor.terrainData.heightmapHeight - 1);
        }
        if (terrain.topNeighbor)
        {
            if (terrain.topNeighbor.leftNeighbor)
                _corner_x = terrain.topNeighbor.leftNeighbor.terrainData.GetHeight(terrain.topNeighbor.leftNeighbor.terrainData.heightmapWidth - 1, 0);

            if (terrain.topNeighbor.rightNeighbor)
                _corner_y = terrain.topNeighbor.rightNeighbor.terrainData.GetHeight(0, 0);
        }
        if (terrain.rightNeighbor)
        {
            if (terrain.rightNeighbor.topNeighbor)
                _corner_x = terrain.rightNeighbor.topNeighbor.terrainData.GetHeight(terrain.rightNeighbor.topNeighbor.terrainData.heightmapWidth - 1, 0);

            if (terrain.rightNeighbor.bottomNeighbor)
                _corner_z = terrain.rightNeighbor.bottomNeighbor.terrainData.GetHeight(0, terrain.rightNeighbor.bottomNeighbor.terrainData.heightmapHeight - 1);
        }
        if (terrain.bottomNeighbor)
        {
            if(terrain.bottomNeighbor.rightNeighbor)
                _corner_z = terrain.bottomNeighbor.rightNeighbor.terrainData.GetHeight(0, terrain.bottomNeighbor.rightNeighbor.terrainData.heightmapHeight - 1);

            if(terrain.bottomNeighbor.leftNeighbor)
                _corner_w = terrain.bottomNeighbor.leftNeighbor.terrainData.GetHeight(terrain.bottomNeighbor.leftNeighbor.terrainData.heightmapWidth - 1, terrain.bottomNeighbor.leftNeighbor.terrainData.heightmapHeight - 1);
        }


        computeShader.SetVector(NameIDs.terrainSize, terrainSize);
        computeShader.SetVector(NameIDs.terrainPosition, terrainPosition);
        computeShader.SetInt(NameIDs.terrainHeightMapResolution, terrainHeightMapResolution);
        computeShader.SetInt(NameIDs.alphaMapResolution, alphamapResolution);
        computeShader.SetVector(NameIDs.hasNeighborTerrains, hasNeighborTerrains);
        computeShader.SetVector(NameIDs.cornerNeighborTerrainsHeights, cornerNeighborTerrainsHeights);
    }



    public void Generate_Height_Map()
    {
        AssignBuffersAndParametersFor_Generate_Height_Map_Kernel();

        computeShader.Dispatch(NameIDs.Generate_Height_Map, (hm_x + extraThread) / 8, (hm_y + extraThread) / 8, 1);
    }



















    void AssignBuffersAndParametersFor_Generate_NeighborTerrain_Height_Map_Kernel()
    {


        if (terrain.leftNeighbor)
            computeShader.SetTexture(NameIDs.Generate_NeighborTerrain_Height_Map, NameIDs.unity_heightMap_left, CopyRenderTexture(terrain.leftNeighbor.gameObject.GetComponent<TerrainPainter_Terrain>().renderTexture_height_slope_snowWeight_Maps));
        else
            computeShader.SetTexture(NameIDs.Generate_NeighborTerrain_Height_Map, NameIDs.unity_heightMap_left, CopyRenderTexture(null));


        if (terrain.rightNeighbor)
            computeShader.SetTexture(NameIDs.Generate_NeighborTerrain_Height_Map, NameIDs.unity_heightMap_right, CopyRenderTexture(terrain.rightNeighbor.gameObject.GetComponent<TerrainPainter_Terrain>().renderTexture_height_slope_snowWeight_Maps));
        else
            computeShader.SetTexture(NameIDs.Generate_NeighborTerrain_Height_Map, NameIDs.unity_heightMap_right, CopyRenderTexture(null));


        if (terrain.bottomNeighbor)
            computeShader.SetTexture(NameIDs.Generate_NeighborTerrain_Height_Map, NameIDs.unity_heightMap_down, CopyRenderTexture(terrain.bottomNeighbor.gameObject.GetComponent<TerrainPainter_Terrain>().renderTexture_height_slope_snowWeight_Maps));
        else
            computeShader.SetTexture(NameIDs.Generate_NeighborTerrain_Height_Map, NameIDs.unity_heightMap_down, CopyRenderTexture(null));


        if (terrain.topNeighbor)
            computeShader.SetTexture(NameIDs.Generate_NeighborTerrain_Height_Map, NameIDs.unity_heightMap_up, CopyRenderTexture(terrain.topNeighbor.gameObject.GetComponent<TerrainPainter_Terrain>().renderTexture_height_slope_snowWeight_Maps));
        else
            computeShader.SetTexture(NameIDs.Generate_NeighborTerrain_Height_Map, NameIDs.unity_heightMap_up, CopyRenderTexture(null));




        computeShader.SetTexture(NameIDs.Generate_NeighborTerrain_Height_Map, NameIDs.neighbor_terrain_heightMaps, renderTexture_neighbor_terrain_heightMaps);

    }






    public void Generate_NeighborTerrain_Height_Map()
    {
        AssignBuffersAndParametersFor_Generate_NeighborTerrain_Height_Map_Kernel();

        computeShader.Dispatch(NameIDs.Generate_NeighborTerrain_Height_Map, (hm_x + extraThread) / 8, (hm_y + extraThread) / 8, 1);
    }














    void AssignBuffersAndParametersFor_Generate_Slope_Map_Kernel()
    {
        computeShader.SetTexture(NameIDs.Generate_Slope_Map, NameIDs.height_slope_snowWeight_Maps, renderTexture_height_slope_snowWeight_Maps);
        computeShader.SetTexture(NameIDs.Generate_Slope_Map, NameIDs.neighbor_terrain_heightMaps, renderTexture_neighbor_terrain_heightMaps);
    }


    public void Generate_Slope_Map()
    {
        AssignBuffersAndParametersFor_Generate_Slope_Map_Kernel();

        computeShader.Dispatch(NameIDs.Generate_Slope_Map, (hm_x + extraThread) / 8, (hm_y + extraThread) / 8, 1);
    }











    void AssignBuffersAndParametersFor_Generate_SnowWeight_Map_Kernel()
    {
        computeShader.SetTexture(NameIDs.Generate_SnowWeight_Map, NameIDs.height_slope_snowWeight_Maps, renderTexture_height_slope_snowWeight_Maps);
    }


    public void Generate_SnowWeight_Maps()
    {
        AssignBuffersAndParametersFor_Generate_SnowWeight_Map_Kernel();

        computeShader.Dispatch(NameIDs.Generate_SnowWeight_Map, (hm_x + extraThread) / 8, (hm_y + extraThread) / 8, 1);
    }












    void AssignBuffersAndParametersFor_Generate_FlowMap_AddWater_Kernel()
    {
        renderTexture_waterOutMap_this = CreateRenderTexture(hm_x);

        computeShader.SetInt(NameIDs.flowMapIteration, flowMapIteration);

        computeShader.SetTexture(NameIDs.FlowMap_AddWater, NameIDs.height_slope_snowWeight_Maps, renderTexture_height_slope_snowWeight_Maps);
    }

    public void FlowMap_AddWater()
    {
        flowMapIteration = manager.flowMapIteration;

        AssignBuffersAndParametersFor_Generate_FlowMap_AddWater_Kernel();

        computeShader.Dispatch(NameIDs.FlowMap_AddWater, (hm_x + extraThread) / 8, (hm_y + extraThread) / 8, 1);
    }











    void AssignBuffersAndParametersFor_FlowMap_GenerateNeighborTerrainWaterMaps()
    {

        renderTexture_neighbor_terrain_waterMaps = CreateRenderTexture(hm_x, RenderTextureFormat.ARGB64);

        renderTexture_waterMap_left = (terrain.leftNeighbor != null ? terrain.leftNeighbor.gameObject.GetComponent<TerrainPainter_Terrain>().renderTexture_height_slope_snowWeight_Maps : CreateRenderTexture(hm_x));
        renderTexture_waterMap_up = (terrain.topNeighbor != null ? terrain.topNeighbor.gameObject.GetComponent<TerrainPainter_Terrain>().renderTexture_height_slope_snowWeight_Maps : CreateRenderTexture(hm_x));
        renderTexture_waterMap_right = (terrain.rightNeighbor != null ? terrain.rightNeighbor.gameObject.GetComponent<TerrainPainter_Terrain>().renderTexture_height_slope_snowWeight_Maps : CreateRenderTexture(hm_x));
        renderTexture_waterMap_down = (terrain.bottomNeighbor != null ? terrain.bottomNeighbor.gameObject.GetComponent<TerrainPainter_Terrain>().renderTexture_height_slope_snowWeight_Maps : CreateRenderTexture(hm_x));


        computeShader.SetTexture(NameIDs.FlowMap_GenerateNeighborTerrainWaterMaps, NameIDs.waterMap_left, renderTexture_waterMap_left);
        computeShader.SetTexture(NameIDs.FlowMap_GenerateNeighborTerrainWaterMaps, NameIDs.waterMap_up, renderTexture_waterMap_up);
        computeShader.SetTexture(NameIDs.FlowMap_GenerateNeighborTerrainWaterMaps, NameIDs.waterMap_right, renderTexture_waterMap_right);
        computeShader.SetTexture(NameIDs.FlowMap_GenerateNeighborTerrainWaterMaps, NameIDs.waterMap_down, renderTexture_waterMap_down);
        computeShader.SetTexture(NameIDs.FlowMap_GenerateNeighborTerrainWaterMaps, NameIDs.neighbor_terrain_waterMaps, renderTexture_neighbor_terrain_waterMaps);

    }


    public void FlowMap_GenerateNeighborTerrainWaterMaps()
    {
        AssignBuffersAndParametersFor_FlowMap_GenerateNeighborTerrainWaterMaps();

        computeShader.Dispatch(NameIDs.FlowMap_GenerateNeighborTerrainWaterMaps, (hm_x + extraThread) / 8, (hm_y + extraThread) / 8, 1);
    }













    void AssignBuffersAndParametersFor_Generate_FlowMap_CalculateWaterOut_Kernel()
    {

        renderTexture_waterOutMap_left = (terrain.leftNeighbor != null ? terrain.leftNeighbor.gameObject.GetComponent<TerrainPainter_Terrain>().renderTexture_waterOutMap_this : CreateRenderTexture(hm_x));
        renderTexture_waterOutMap_up = (terrain.topNeighbor != null ? terrain.topNeighbor.gameObject.GetComponent<TerrainPainter_Terrain>().renderTexture_waterOutMap_this : CreateRenderTexture(hm_x));
        renderTexture_waterOutMap_right = (terrain.rightNeighbor != null ? terrain.rightNeighbor.gameObject.GetComponent<TerrainPainter_Terrain>().renderTexture_waterOutMap_this : CreateRenderTexture(hm_x));
        renderTexture_waterOutMap_down = (terrain.bottomNeighbor != null ? terrain.bottomNeighbor.gameObject.GetComponent<TerrainPainter_Terrain>().renderTexture_waterOutMap_this : CreateRenderTexture(hm_x));


        computeShader.SetTexture(NameIDs.FlowMap_CalculateWaterOut, NameIDs.height_slope_snowWeight_Maps, renderTexture_height_slope_snowWeight_Maps);
        computeShader.SetTexture(NameIDs.FlowMap_CalculateWaterOut, NameIDs.neighbor_terrain_heightMaps, renderTexture_neighbor_terrain_heightMaps);
        computeShader.SetTexture(NameIDs.FlowMap_CalculateWaterOut, NameIDs.neighbor_terrain_waterMaps, renderTexture_neighbor_terrain_waterMaps);
        computeShader.SetTexture(NameIDs.FlowMap_CalculateWaterOut, NameIDs.waterOutMap_this, renderTexture_waterOutMap_this);
        computeShader.SetTexture(NameIDs.FlowMap_CalculateWaterOut, NameIDs.waterOutMap_left, renderTexture_waterOutMap_left);
        computeShader.SetTexture(NameIDs.FlowMap_CalculateWaterOut, NameIDs.waterOutMap_up, renderTexture_waterOutMap_up);
        computeShader.SetTexture(NameIDs.FlowMap_CalculateWaterOut, NameIDs.waterOutMap_right, renderTexture_waterOutMap_right);
        computeShader.SetTexture(NameIDs.FlowMap_CalculateWaterOut, NameIDs.waterOutMap_down, renderTexture_waterOutMap_down);

    }


    public void FlowMap_CalculateWaterOut()
    {
        AssignBuffersAndParametersFor_Generate_FlowMap_CalculateWaterOut_Kernel();

        computeShader.Dispatch(NameIDs.FlowMap_CalculateWaterOut, (hm_x + extraThread) / 8, (hm_y + extraThread) / 8, 1);
    }











    void AssignBuffersAndParametersFor_Generate_FlowMap_MoveWater_Kernel()
    {
        computeShader.SetTexture(NameIDs.FlowMap_MoveWater, NameIDs.height_slope_snowWeight_Maps, renderTexture_height_slope_snowWeight_Maps);
        computeShader.SetTexture(NameIDs.FlowMap_MoveWater, NameIDs.neighbor_terrain_waterMaps, renderTexture_neighbor_terrain_waterMaps);
        computeShader.SetTexture(NameIDs.FlowMap_MoveWater, NameIDs.waterOutMap_this, renderTexture_waterOutMap_this);
        computeShader.SetTexture(NameIDs.FlowMap_MoveWater, NameIDs.waterOutMap_left, renderTexture_waterOutMap_left);
        computeShader.SetTexture(NameIDs.FlowMap_MoveWater, NameIDs.waterOutMap_up, renderTexture_waterOutMap_up);
        computeShader.SetTexture(NameIDs.FlowMap_MoveWater, NameIDs.waterOutMap_right, renderTexture_waterOutMap_right);
        computeShader.SetTexture(NameIDs.FlowMap_MoveWater, NameIDs.waterOutMap_down, renderTexture_waterOutMap_down);
    }

    public void FlowMap_MoveWater()
    {
        AssignBuffersAndParametersFor_Generate_FlowMap_MoveWater_Kernel();

        computeShader.Dispatch(NameIDs.FlowMap_MoveWater, (hm_x + extraThread) / 8, (hm_y + extraThread) / 8, 1);
    }












    void AssignBuffersAndParametersFor_Generate_FlowMap_Generate_Kernel()
    {
        computeShader.SetTexture(NameIDs.FlowMap_Generate, NameIDs.convexity_concavitiy_flow_Maps, renderTexture_convexity_concavitiy_flow_Maps);
        computeShader.SetTexture(NameIDs.FlowMap_Generate, NameIDs.waterOutMap_this, renderTexture_waterOutMap_this);
        computeShader.SetTexture(NameIDs.FlowMap_Generate, NameIDs.waterOutMap_left, renderTexture_waterOutMap_left);
        computeShader.SetTexture(NameIDs.FlowMap_Generate, NameIDs.waterOutMap_up, renderTexture_waterOutMap_up);
        computeShader.SetTexture(NameIDs.FlowMap_Generate, NameIDs.waterOutMap_right, renderTexture_waterOutMap_right);
        computeShader.SetTexture(NameIDs.FlowMap_Generate, NameIDs.waterOutMap_down, renderTexture_waterOutMap_down);
    }

    public void FlowMap_Generate()
    {
        AssignBuffersAndParametersFor_Generate_FlowMap_Generate_Kernel();

        computeShader.Dispatch(NameIDs.FlowMap_Generate, (hm_x + extraThread) / 8, (hm_y + extraThread) / 8, 1);
    }












    public void ReleaseRenderTexturesForWaterMap()
    {
        renderTexture_neighbor_terrain_waterMaps.Release();
        renderTexture_waterOutMap_this.Release();
        renderTexture_waterOutMap_up.Release();
        renderTexture_waterOutMap_right.Release();
        renderTexture_waterOutMap_down.Release();
        
    }









    void AssignBuffersAndParametersFor_CurvatureMap_Kernel()
    {
        computeShader.SetTexture(NameIDs.CurvatureMap_Generate, NameIDs.height_slope_snowWeight_Maps, renderTexture_height_slope_snowWeight_Maps);
        computeShader.SetTexture(NameIDs.CurvatureMap_Generate, NameIDs.neighbor_terrain_heightMaps, renderTexture_neighbor_terrain_heightMaps);
        computeShader.SetTexture(NameIDs.CurvatureMap_Generate, NameIDs.convexity_concavitiy_flow_Maps, renderTexture_convexity_concavitiy_flow_Maps);
    }


    public void CurvatureMap_Generate()
    {
        AssignBuffersAndParametersFor_CurvatureMap_Kernel();

        computeShader.Dispatch(NameIDs.CurvatureMap_Generate, (hm_x + extraThread) / 8, (hm_y + extraThread) / 8, 1);
    }

















    void AssignBuffersAndParametersFor_Generate_SplatMap_Kernel()
    {
        if(snowAmount > 0)
            computeShader.SetFloat(NameIDs.snowAmount, snowAmount);
        else
            computeShader.SetFloat(NameIDs.snowAmount, -1f);


        computeShader.SetTexture(NameIDs.Generate_SplatMap, NameIDs.height_slope_snowWeight_Maps, renderTexture_height_slope_snowWeight_Maps);
        computeShader.SetTexture(NameIDs.Generate_SplatMap, NameIDs.convexity_concavitiy_flow_Maps, renderTexture_convexity_concavitiy_flow_Maps);


        splatPaintRulesBuffer.SetData(splatPaintRulesArray);
        computeShader.SetBuffer(NameIDs.Generate_SplatMap, NameIDs.splatPaintRulesBuffer, splatPaintRulesBuffer);


        splat_Map_Total_Weight_Buffer.SetData(splat_Map_Total_Weight);
        computeShader.SetBuffer(NameIDs.Generate_SplatMap, NameIDs.splat_Map_Total_Weight_Buffer, splat_Map_Total_Weight_Buffer);
    }




    public void Generate_SplatMap()
    {
        snowAmount = manager.snowAmount;

        SetUpSplatPaintRulesArray();
        SetUpSplatPaintRulesBuffer();
        SetUpSplatmapTotalWeightBuffer();

        AssignBuffersAndParametersFor_Generate_SplatMap_Kernel();


        for (int i = 0; i < splats.Length; i++)
        {
            ExecuteComputeShader(i);
        }
    }



    void ExecuteComputeShader(int p_splatRuleBufferIndex)
    {
        if (p_splatRuleBufferIndex >= 0)
        {
            computeShader.SetInt(NameIDs.splatType, (int)splats[p_splatRuleBufferIndex].splatType);
            computeShader.SetInt(NameIDs.paintMethod, (int)splats[p_splatRuleBufferIndex].paintMethod);

            computeShader.SetInt(NameIDs.splatRuleBufferIndex, p_splatRuleBufferIndex);
            computeShader.SetTexture(NameIDs.Generate_SplatMap, NameIDs.splatMapsArray, splatMapsArray[Mathf.FloorToInt(((float)p_splatRuleBufferIndex) / 4)]);


            computeShader.Dispatch(NameIDs.Generate_SplatMap, am_x / 8, am_y / 8, 1);

            splat_Map_Total_Weight_Buffer.GetData(splat_Map_Total_Weight);
        }
    }














    void AssignBuffersAndParametersFor_Normalize_SplatMap_Kernel(int p_splatMapArrrayIndex)
    {
        splat_Map_Total_Weight_Buffer.SetData(splat_Map_Total_Weight);
        computeShader.SetBuffer(NameIDs.Normalize_SplatMap, NameIDs.splat_Map_Total_Weight_Buffer, splat_Map_Total_Weight_Buffer);

        computeShader.SetTexture(NameIDs.Normalize_SplatMap, NameIDs.splatMapsArray, splatMapsArray[p_splatMapArrrayIndex]);
    }



    public void Normalize_SplatMap()
    {
        for (int i = 0; i < splatMapsArray.Length; i++)
        {
            AssignBuffersAndParametersFor_Normalize_SplatMap_Kernel(i);

            computeShader.Dispatch(NameIDs.Normalize_SplatMap, am_x / 8, am_y / 8, 1);
        }


        splat_Map_Total_Weight_Buffer.Release();
        splatPaintRulesBuffer.Release();
    }













    public void WriteToTerrainAlphamap()
    {
        for (int i = 0; i < terrain.terrainData.alphamapTextures.Length; i++)
        {
            terrain.terrainData.alphamapTextures[i].SetPixels32(GetPixels32FromRenderTexture(splatMapsArray[i]));
            terrain.terrainData.alphamapTextures[i].Apply();
        }

        terrain.terrainData.SetBaseMapDirty();
    }














    Color32[] GetPixels32FromRenderTexture(RenderTexture p_renderTexture)
    {
        RenderTexture previusRT = RenderTexture.active;

        RenderTexture.active = p_renderTexture;

        Texture2D _newTexture = new Texture2D(p_renderTexture.width, p_renderTexture.height, TextureFormat.ARGB32, false);
        _newTexture.ReadPixels(new Rect(0, 0, _newTexture.width, _newTexture.height), 0, 0);

        RenderTexture.active = previusRT;
        return _newTexture.GetPixels32(0);
    }


    /*
    Texture2D ConvertRenderTextureToTexture2D(RenderTexture p_renderTexture)
    {
        RenderTexture previusRT = RenderTexture.active;

        RenderTexture.active = p_renderTexture;

        Texture2D _newTexture = new Texture2D(p_renderTexture.width, p_renderTexture.height, TextureFormat.ARGB32, false);
        _newTexture.ReadPixels(new Rect(0, 0, _newTexture.width, _newTexture.height), 0, 0);

        RenderTexture.active = previusRT;
        return _newTexture;
    }
    */







}