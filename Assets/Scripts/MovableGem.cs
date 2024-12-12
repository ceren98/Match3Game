using UnityEngine;
using System.Collections;
namespace Match3Game
{
    public class MovableGem : MonoBehaviour 
    {
        
        private Gem gem;

        private void Awake()
        {
            gem = GetComponent<Gem>();
        }




        public void Move(int newX, int newY)
        {
            gem.X = newX;
            gem.Y = newY;

            gem.transform.localPosition = gem.GridRef.GetWorldPosition(newX, newY);
        }




    }
}
