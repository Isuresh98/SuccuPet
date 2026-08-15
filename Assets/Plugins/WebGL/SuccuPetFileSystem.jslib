mergeInto(LibraryManager.library, {
    SuccuPet_SyncFileSystem: function () {
        FS.syncfs(false, function (error) {
            if (error) {
                console.error(
                    "SuccuPet: IndexedDB synchronization failed.",
                    error
                );

                return;
            }

            console.log(
                "SuccuPet: save synchronized to IndexedDB."
            );
        });
    }
});