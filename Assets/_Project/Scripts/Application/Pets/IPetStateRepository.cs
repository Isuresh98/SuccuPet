using SuccuPet.Core.Pets;

namespace SuccuPet.Application.Pets
{
    public interface IPetStateRepository
    {
        bool TryLoad(out PetState petState);
        void Save(PetState petState);
    }
}