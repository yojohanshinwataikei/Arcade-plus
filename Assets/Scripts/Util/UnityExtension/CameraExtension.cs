using UnityEngine;

namespace Arcade.Util.UnityExtension
{
	public static class CameraExtension
    {
        public static Ray ScreenPointToRay(this Camera camera)
        {
            return camera.ScreenPointToRay(Input.mousePosition);
        }
    }
}