using UnityEngine;

[DisallowMultipleComponent]
public class CombatMapView : MonoBehaviour
{
    [SerializeField] private Grid grid;
    [SerializeField] private TileOverlayRenderer tileOverlayRenderer;

    public Grid Grid => grid;
    public TileOverlayRenderer TileOverlayRenderer => tileOverlayRenderer;
}
