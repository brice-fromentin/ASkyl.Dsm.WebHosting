# APIs FileStation - Résumé des implémentations

## Structure des APIs FileStation ajoutées

### Constantes (DsmDefaults.cs)
Les constantes suivantes ont été ajoutées pour définir les APIs FileStation :

```csharp
// FileStation APIs
public const string DsmApiFileStationInfo = "SYNO.FileStation.Info";
public const string DsmApiFileStationList = "SYNO.FileStation.List";
public const string DsmApiFileStationSearch = "SYNO.FileStation.Search";
public const string DsmApiFileStationVirtualFolder = "SYNO.FileStation.VirtualFolder";
public const string DsmApiFileStationFavorite = "SYNO.FileStation.Favorite";
public const string DsmApiFileStationThumb = "SYNO.FileStation.Thumb";
public const string DsmApiFileStationDirSize = "SYNO.FileStation.DirSize";
public const string DsmApiFileStationMd5 = "SYNO.FileStation.MD5";
public const string DsmApiFileStationCheckPermission = "SYNO.FileStation.CheckPermission";
public const string DsmApiFileStationUpload = "SYNO.FileStation.Upload";
public const string DsmApiFileStationDownload = "SYNO.FileStation.Download";
public const string DsmApiFileStationSharing = "SYNO.FileStation.Sharing";
public const string DsmApiFileStationCreateFolder = "SYNO.FileStation.CreateFolder";
public const string DsmApiFileStationRename = "SYNO.FileStation.Rename";
public const string DsmApiFileStationCopyMove = "SYNO.FileStation.CopyMove";
public const string DsmApiFileStationDelete = "SYNO.FileStation.Delete";
public const string DsmApiFileStationExtract = "SYNO.FileStation.Extract";
public const string DsmApiFileStationCompress = "SYNO.FileStation.Compress";
public const string DsmApiFileStationBackgroundTask = "SYNO.FileStation.BackgroundTask";
```

### Définitions des modèles de données (/API/Definitions/)

#### Modèles de base
- `FileStationFile.cs` - Représentation d'un fichier/dossier
- `FileStationFileAdditional.cs` - Informations supplémentaires (taille, permissions, etc.)
- `FileStationOwner.cs` - Informations de propriétaire
- `FileStationTime.cs` - Horodatages (création, modification, accès)
- `FileStationPermission.cs` - Permissions et ACLs
- `FileStationAcl.cs` - Contrôles d'accès détaillés

#### Modèles de requêtes
- `FileStationListRequest.cs` - Paramètres pour lister les fichiers
- `FileStationSearchRequest.cs` - Paramètres de recherche avancée
- `FileStationCreateFolderRequest.cs` - Création de dossiers
- `FileStationDeleteRequest.cs` - Suppression de fichiers/dossiers
- `FileStationRenameRequest.cs` - Renommage
- `FileStationCopyMoveRequest.cs` - Copie et déplacement
- `FileStationCompressRequest.cs` - Compression d'archives
- `FileStationExtractRequest.cs` - Extraction d'archives
- `FileStationCheckPermissionRequest.cs` - Vérification de permissions
- `FileStationDirSizeRequest.cs` - Calcul de taille de dossiers
- `FileStationVirtualFolderRequest.cs` - Dossiers virtuels (montages réseau)
- `FileStationFavoriteRequest.cs` - Gestion des favoris
- `FileStationThumbRequest.cs` - Génération de vignettes
- `FileStationMd5Request.cs` - Calcul de hash MD5
- `FileStationSharingRequest.cs` - Partage de liens
- `FileStationDownloadRequest.cs` - Téléchargement de fichiers
- `FileStationBackgroundTaskRequest.cs` - Gestion des tâches d'arrière-plan

### Paramètres des APIs (/API/Parameters/FileStationAPI/)

#### APIs principales
- `FileStationInfoParameters.cs` - Informations sur FileStation
- `FileStationListParameters.cs` - Listing des fichiers et dossiers
- `FileStationSearchParameters.cs` - Recherche (start, list, stop)
- `FileStationCreateFolderParameters.cs` - Création de dossiers
- `FileStationRenameParameters.cs` - Renommage
- `FileStationDeleteParameters.cs` - Suppression
- `FileStationCopyMoveParameters.cs` - Copie/déplacement (start, status, stop)

#### APIs avancées
- `FileStationCheckPermissionParameters.cs` - Vérification de permissions
- `FileStationDirSizeParameters.cs` - Calcul de taille (start, status, stop)
- `FileStationCompressParameters.cs` - Compression (start, status, stop)
- `FileStationExtractParameters.cs` - Extraction (start, status, stop, list)
- `FileStationThumbParameters.cs` - Génération de vignettes
- `FileStationMd5Parameters.cs` - Calcul MD5

#### APIs de gestion
- `FileStationVirtualFolderParameters.cs` - Dossiers virtuels
- `FileStationFavoriteParameters.cs` - Favoris (list, add, delete, edit)
- `FileStationSharingParameters.cs` - Partage (list, create, delete, edit, clear_invalid)
- `FileStationDownloadParameters.cs` - Téléchargement
- `FileStationBackgroundTaskParameters.cs` - Tâches d'arrière-plan (list, clear_finished)

### Réponses des APIs (/API/Responses/)

#### Réponses principales
- `FileStationListResponse.cs` - Résultats de listing
- `FileStationSearchResponse.cs` - Résultats de recherche
- `FileStationOperationResponses.cs` - Opérations CRUD (create, rename, delete, copy/move)

#### Réponses avancées
- `FileStationAdditionalResponses.cs` - Info, permissions, taille de dossiers
- `FileStationVirtualFolderAndFavoriteResponses.cs` - Dossiers virtuels et favoris
- `FileStationSharingAndTaskResponses.cs` - Partage, MD5, et tâches d'arrière-plan

## Utilisation

Toutes les APIs FileStation suivent le même pattern que les APIs ReverseProxy existantes :

```csharp
// Exemple d'utilisation - Lister les fichiers
var parameters = new FileStationListParameters(client.ApiInformations);
parameters.Parameters.FolderPath = "/volume1/homes/user";
parameters.Parameters.Additional = "real_path,size,owner,time,perm";

var response = await client.ExecuteAsync<FileStationListResponse>(parameters);
if (response?.Success == true && response.Data?.Files != null)
{
    foreach (var file in response.Data.Files)
    {
        Console.WriteLine($"{file.Name} - {file.Type} - {file.Additional?.Size} bytes");
    }
}
```

```csharp
// Exemple d'utilisation - Recherche de fichiers
var searchParams = new FileStationSearchStartParameters(client.ApiInformations);
searchParams.Parameters.FolderPath = "/volume1";
searchParams.Parameters.Pattern = "*.pdf";
searchParams.Parameters.Recursive = true;

var searchResponse = await client.ExecuteAsync<FileStationSearchResponse>(searchParams);
```

## Fonctionnalités couvertes

✅ **Gestion des fichiers et dossiers**
- Listing avec filtres et tri
- Création, renommage, suppression
- Copie et déplacement
- Recherche avancée

✅ **Opérations avancées**
- Compression et extraction d'archives
- Calcul de hash MD5
- Génération de vignettes
- Calcul de taille de dossiers

✅ **Gestion des permissions**
- Vérification des droits d'accès
- Support ACL et permissions POSIX

✅ **Fonctionnalités réseau**
- Dossiers virtuels (montages CIFS, NFS, etc.)
- Partage de liens avec mot de passe et expiration

✅ **Interface utilisateur**
- Gestion des favoris
- Tâches d'arrière-plan

✅ **Téléchargement**
- API de téléchargement de fichiers

Toutes les APIs respectent le style et les patterns établis dans le projet existant avec les APIs ReverseProxy.
