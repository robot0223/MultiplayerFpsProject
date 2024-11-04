
using UnityEngine;


namespace FPS_personal_project
{

   
    public class UIKillFeed : MonoBehaviour
    {
        public UIKillFeedItem killFeedItem;
        public float FeedLifetime = 6f;
       // public Sprite[] WeaponIcons;

        public void ShowKill(string killer, string victim, EWeaponType weaponType, bool isCriticalKill)
        {
            var killFeed = Instantiate(killFeedItem,transform);

            killFeed.Killer.text = killer;
            killFeed.Victim.text = victim;
            //<TODO:this requires weapon icons to be sorted in inspector by human, later fix this to auto sort.>
           // killFeed.WeaponIcon.sprite = WeaponIcons[(int)weaponType];
            killFeed.CriticalKillGroup.SetActive(isCriticalKill);

            // Kill feed item is fading in time automatically via animation component.
            // Make sure the item gets destroyed after the animation is done.
            Destroy(killFeed.gameObject, FeedLifetime);

        }
    }

}
