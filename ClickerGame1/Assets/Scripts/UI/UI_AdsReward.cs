using UnityEngine;

public class UI_AdReward : MonoBehaviour
{
    const int rewardAmount = 50;

    public void OnClckButton()
    {
        AdsManager.Instance.ShowRewardedAd(() =>
        {
            GameManager.Instance.Crystal += rewardAmount;
            SaveManager.Instance?.Save();
        });
    }
}
