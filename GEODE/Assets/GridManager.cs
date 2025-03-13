using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : NetworkBehaviour
{
    //Instance
    public static GridManager Instance;


    [HideInInspector] public Vector3Int currentMousePosition;
    [Header("Highlight Tiles")]
    [SerializeField] private Tile greenTile;
    [SerializeField] private Tile redTile;
    [SerializeField] private int highlightRadius;
    [SerializeField] private Tilemap backgroundTilemap;
    [SerializeField] private Tilemap mainTilemap;
    [SerializeField] private Tilemap highlightTilemap;
    [SerializeField] private GameObject structurePreviewObject;
    [Header("Settings")]
    [SerializeField] private LayerMask structureLayer;
    

    //Internal
    [HideInInspector] public bool holdingStructure;
    public int currentItemId = -1;
    private int previousItemId = -1;
    StructureItem structurePreview;
   
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if(holdingStructure)
        {
            highlightTilemap.gameObject.SetActive(true);
            highlightTilemap.ClearAllTiles();
            //currentItemId is set externally by PlayerInventory, this is probably not ideal.
            if(structurePreviewObject.activeSelf == false || currentItemId != previousItemId)
            {
                structurePreviewObject.SetActive(true);
                previousItemId = currentItemId;
                BaseItem itemPreview = ItemDatabase.Instance.GetItem(currentItemId);
                structurePreview = itemPreview as StructureItem;
                if(structurePreview != null)
                {
                    structurePreviewObject.GetComponent<SpriteRenderer>().sprite = structurePreview.Icon;
                }
            }
            if(structurePreviewObject.activeSelf == true)
            {
                Cursor.visible = false;
                //structurePreviewObject.transform.position = new Vector3Int(currentMousePosition.x - structurePreview.width/2, currentMousePosition.y-structurePreview.height/2, 0);
                structurePreviewObject.transform.position = currentMousePosition;
            }
            //this is so ass to look at, but basically:
            //need to center our highlight area around the item being placed
                //offset the mouse position by the radius of the highlight + the width of the structure/2, this centers it.
                //we need to do this because the middle of the highlight area is originally not the middle of the item, its the bottom left of the item.
            for(int x = currentMousePosition.x - highlightRadius + structurePreview.width/2; x < currentMousePosition.x + highlightRadius + structurePreview.width/2; x++)
            {
                for(int y = currentMousePosition.y - highlightRadius + structurePreview.height/2; y < currentMousePosition.y + highlightRadius + structurePreview.height/2; y++)
                { 
                    //iterate through and check the occupancy at each position, set its tile based on outcome
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    if(IsPositionOccupied(pos))
                    {
                        highlightTilemap.SetTile(pos, redTile);
                    }
                    else
                    {
                        highlightTilemap.SetTile(pos, greenTile);
                    }
                }
            }
            
        }
        else
        {
            highlightTilemap.gameObject.SetActive(false);
            currentItemId = -1;
            Cursor.visible = true;
            if(structurePreviewObject.activeSelf == true)
            {
                structurePreviewObject.SetActive(false);
            }
        }
    }   

    [ServerRpc(RequireOwnership = false)]
    public void PlaceObjectOnGridServerRpc(int itemId, Vector3Int position, Vector3[] positionsToBlock)
    {
        
        BaseItem baseItem = ItemDatabase.Instance.GetItem(itemId);
        StructureItem structureItem = baseItem as StructureItem;
        if(structureItem != null)
        {
            //Vector3 placePos = new Vector3(position.x-structureItem.width/2, position.y-structureItem.height/2, 0);
            //GameObject newObject = Instantiate(structureItem.prefab, placePos, Quaternion.identity);
            GameObject newObject = Instantiate(structureItem.prefab, position, Quaternion.identity);
            foreach (Vector3 pos in positionsToBlock)
            {
                FlowFieldManager.Instance.SetWalkable(pos, false);
            } 
            FlowFieldManager.Instance.CalculateFlowField();
            newObject.GetComponent<NetworkObject>().Spawn();
            
        }
        else
        {
            Debug.Log("Error. Item is not a structure.");
        }
    }

    public bool IsPositionOccupied(Vector3Int position)
    {
        //raycast at the target location
        Collider2D colhit = Physics2D.OverlapPoint(new Vector2(position.x+.5f, position.y+.5f), structureLayer);
        if(colhit == null)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public void UpdateMousePos(Vector3Int mousePos)
    {
        currentMousePosition = mousePos;
    }

}
