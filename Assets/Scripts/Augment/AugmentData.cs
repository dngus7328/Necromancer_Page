using UnityEngine;

public enum AugmentType
{
    Support,
    Core,
    Mutation
}

[CreateAssetMenu(
    fileName = "NewAugment",
    menuName = "Necromancer Page/Augment Data"
)]
public class AugmentData : ScriptableObject
{
    [Header("기본 정보")]

    [SerializeField]
    private string augmentName;

    [TextArea(2, 5)]
    [SerializeField]
    private string description;

    [SerializeField]
    private AugmentType augmentType =
        AugmentType.Support;

    [Header("중복")]

    [Tooltip(
        "한 번 획득한 뒤 다시 등장할 수 있는지 여부"
    )]
    [SerializeField]
    private bool canRepeat = false;

    public string AugmentName =>
        augmentName;

    public string Description =>
        description;

    public AugmentType AugmentType =>
        augmentType;

    public bool CanRepeat =>
        canRepeat;
}