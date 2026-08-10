namespace SuccuPet.Core.Pets
{
    public enum PetCareActionType
    {
        Feed = 0,
        Sleep = 1,
        Play = 2,
        Bathe = 3,

        // Legacy aliases preserve compatibility with existing code and scenes.
        Rest = Sleep,
        Clean = Bathe
    }
}