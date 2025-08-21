using TMPro;
using UnityEngine;

public class StatsPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text txtHealth;
    [SerializeField] private TMP_Text txtArmor;
    [SerializeField] private TMP_Text txtSpeed;
    [SerializeField] private PreviewAssembler assembler;

    public void UpdateStats()
    {
        if (!assembler) return;

        var f = assembler.GetCurrentFrame();
        if (txtHealth) txtHealth.text = $"Health: {f.baseHealth}";
        if (txtArmor) txtArmor.text = $"Armor: {f.baseArmor}";
        if (txtSpeed) txtSpeed.text = $"Speed: {f.baseSpeed}";
    }
}
