using System;
using System.IO;
using SuccuPet.Application.Pets;
using SuccuPet.Core.Pets;
using SuccuPet.Infrastructure.Persistence;
using UnityEngine;

namespace SuccuPet.Infrastructure.Persistence.Pets
{
    public sealed class JsonFilePetStateRepository :
        IPetStateRepository
    {
        private readonly string filePath;

        public string FilePath => filePath;

        public JsonFilePetStateRepository(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException(
                    "Save file name cannot be empty.",
                    nameof(fileName));
            }

            filePath = Path.Combine(
                UnityEngine.Application.persistentDataPath,
                fileName);
        }

        public bool TryLoad(out PetState petState)
        {
            if (!File.Exists(filePath))
            {
                petState = null;
                return false;
            }

            string json = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidDataException(
                    "Pet save file is empty.");
            }

            PetSaveData saveData =
                JsonUtility.FromJson<PetSaveData>(json);

            petState = PetStateSaveMapper.ToDomain(saveData);

            return true;
        }

        public void Save(PetState petState)
        {
            if (petState == null)
            {
                throw new ArgumentNullException(nameof(petState));
            }

            PetSaveData saveData =
                PetStateSaveMapper.ToSaveData(petState);

            string json = JsonUtility.ToJson(
                saveData,
                prettyPrint: true);

            string directoryPath =
                Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string temporaryPath = filePath + ".tmp";

            try
            {
                File.WriteAllText(temporaryPath, json);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                File.Move(temporaryPath, filePath);

                WebGLFileSystemSync.FlushPendingWrites();
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
    }
}