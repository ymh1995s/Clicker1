using UnityEngine;

public class UI_AdReward : MonoBehaviour
{
    [SerializeField]
    int rewardAmount = 5;

    public void OnClckButton()
    {
        AdsManager.Instance.ShowRewardedAd(() =>
        {
            GameManager.Instance.Crystal += rewardAmount;
            SaveManager.Instance?.Save();
        });
    }
}
