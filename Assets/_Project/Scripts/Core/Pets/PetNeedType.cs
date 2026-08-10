namespace SuccuPet.Core.Pets
{
    public enum PetNeedType
    {
        Vitality = 0,
        Rest = 1,
        Mood = 2,
        Allure = 3,

        // Legacy aliases preserve compatibility with existing code and saves.
        Fullness = Vitality,
        Energy = Rest,
        Happiness = Mood,
        Hygiene = Allure
    }
}