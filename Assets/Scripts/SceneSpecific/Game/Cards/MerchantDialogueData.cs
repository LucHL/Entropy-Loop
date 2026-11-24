using System;

[Serializable]
public class MerchantDialogueData
{
    public string merchantName;
    public string merchantTitle;
    public string portrait;
    // une phrase tirée au hasard à chaque ouverture du shop
    public string[] greetings;
    public string[] purchase;
    public string[] cantAfford;
}
