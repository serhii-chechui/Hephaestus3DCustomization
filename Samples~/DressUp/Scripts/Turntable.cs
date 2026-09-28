using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D.Samples.DressUp
{
    /// <summary>Turns the object around its vertical axis, so the outfit is seen from every side.</summary>
    public class Turntable : MonoBehaviour
    {
        [SerializeField]
        private float _degreesPerSecond = 20f;

        private void Update()
        {
            transform.Rotate(0f, _degreesPerSecond * Time.deltaTime, 0f, Space.World);
        }
    }
}
