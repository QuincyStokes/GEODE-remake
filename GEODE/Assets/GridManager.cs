using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : NetworkBehaviour
{
    public static GridManager Instance;
    [HideInInspector] public bool holdingStructure;
    private Vector3Int currentHoveredTile;
    [HideInInspector] public Vector3Int currentMousePosition;
    [Header("Highlight Tiles")]
    [SerializeField] private Tile greenTile;
    [SerializeField] private Tile redTile;
    [SerializeField] private int highlightRadius;
    [SerializeField] private Tilemap backgroundTilemap;
    [SerializeField] private Tilemap mainTilemap;
    [SerializeField] private Tilemap highlightTilemap;

    [Header("Settings")]
    [SerializeField] private LayerMask structureLayer;

   
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
            //This is where we will
            //if(currentHoveredTile != ) hmm unsure of how to update the position if we're only getting it
            //via the playercontroller, maybe an event?
            //for now maybe we go 7x7 with the mouse position as middle
            highlightTilemap.ClearAllTiles();
            for(int x = currentMousePosition.x - highlightRadius; x < currentMousePosition.x + highlightRadius; x++)
            {
                for(int y = currentMousePosition.y - highlightRadius; y < currentMousePosition.y + highlightRadius; y++)
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
        }
    }   

    [ServerRpc(RequireOwnership = false)]
    public void PlaceObjectServerRpc(int itemId, Vector3Int position)
    {
        
        BaseItem baseItem = ItemDatabase.Instance.GetItem(itemId);
        StructureItem structureItem = baseItem as StructureItem;
        if(structureItem != null)
        {
            GameObject newObject = Instantiate(structureItem.prefab, position, Quaternion.identity);
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
