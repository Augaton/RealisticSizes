# RealisticSizes 4.0

> Portage EXILED 9.14.2 d'un plugin de **JesusQC**. Depot non affilie a
> l'auteur d'origine. Voir [NOTICE.md](NOTICE.md) pour l'attribution.

Donne a chaque joueur une taille legerement differente a l'apparition.

**EXILED 9.14.2** — `dotnet build -c Release RealisticSizes/RealisticSizes.csproj`

## Modes

| Mode | Effet |
|---|---|
| `Roleplay` | Variations discretes, credibles en RP (defaut) |
| `Fun` | Variations larges et visibles |
| `Manual` | Une plage par role, definie dans `manual_ranges` |

`allow_unproportional_values` autorise une largeur differente de la hauteur.
A `false`, la silhouette reste proportionnee.

## Configuration

| Cle | Defaut | Role |
|---|---|---|
| `affect_scps` | `false` | Deconseille : modifier la taille d'un SCP change ses hitbox |
| `ignored_roles` | Tutorial, Overwatch, Filmmaker, SCP-079 | Roles jamais redimensionnes |
| `apply_delay` | `0.4` | Delai avant application, le temps que le spawn se termine |
| `spread_workload` | `true` | Etale les applications sur plusieurs frames en spawn massif |

Les plages sont des objets typees (`min_height`, `max_height`, `min_width`,
`max_width`) et non plus une chaine `"0.5:0.5::1.15:1.15"`.

## Dependances

Ce plugin depend de **AugatonLib**, la bibliotheque partagee de la
collection.

| Fichier | Destination |
|---|---|
| `RealisticSizes.dll` | `Plugins/7777/` |
| `AugatonLib.dll` | `Plugins/dependencies/` |
| HintServiceMeow | `Plugins/7777/` |

`AugatonLib.dll` ne va **jamais** dans `Plugins/7777/` : EXILED
tenterait de le charger comme plugin. Il doit etre deploye avant ce plugin et
mis a jour en meme temps.

Pour compiler ce depot isolement, cloner
[AugatonLib](https://github.com/Augaton/AugatonLib) a cote,
ou passer `-p:CommonProject=chemin/vers/AugatonLib.csproj`.

## Commandes staff

| Commande | Permission | Effet |
|---|---|---|
| `sizes status` | `realisticsizes.manage` | Mode actif et reglages courants |
| `sizes reset <joueur\|*>` | `realisticsizes.manage` | Remet la taille normale |

Alias `rsize`.

Toutes les commandes de la collection partagent le meme socle : verification de
permission en premiere ligne, arguments bornes en longueur, exceptions
capturees, actions a impact tracees avec l'auteur. Une commande parente sans
argument liste ses sous-commandes.

## Installation depuis une release

Chaque tag `v*` declenche une release qui publie une archive **contenant deja
AugatonLib**. Extraire `RealisticSizes.zip` dans `.config/EXILED/` :

```
Plugins/7777/RealisticSizes.dll
Plugins/dependencies/AugatonLib.dll
```

Les DLL sont aussi publiees separement pour une mise a jour ciblee.

Si plusieurs plugins de la collection sont installes, garder la version
d'AugatonLib la plus recente : elle est partagee par tous.

## Integration continue

| Workflow | Declencheur | Role |
|---|---|---|
| `build` | push sur `main`, pull request | Compile le plugin contre AugatonLib et verifie la sortie |
| `release` | tag `v*` | Compile, empaquette avec AugatonLib et publie la release |

La CI recupere AugatonLib par `actions/checkout` sur le depot
[Augaton/AugatonLib](https://github.com/Augaton/AugatonLib), branche `main` par
defaut. Le declenchement manuel de `release` permet de fixer une autre version
via l'entree `augatonlib_ref`.

Gitleaks tourne sur chaque push et bloque en cas de secret detecte.

## Note de portage

La version 3.x ciblait EXILED 2.3.4. Quatre defauts corriges :

- **`float.Parse` sans culture invariante.** Les tailles etaient stockees en
  chaine puis parsees avec la culture du systeme. Sur une machine francaise,
  ou la virgule est le separateur decimal, `"1.15"` ne se parse pas comme
  attendu. Le format chaine a disparu au profit d'un objet typee.
- **Condition inversee.** `if (ev.Player != null || ev.NewRole != RoleType.None)`
  utilisait `||` la ou `&&` etait attendu : un `Player` nul passait la garde et
  provoquait une exception.
- **Course entre joueurs.** Les tailles calculees etaient stockees dans deux
  champs d'instance partages par tous les joueurs. Combine au delai aleatoire de
  0,4 a 5 secondes de l'anti-lag, deux apparitions simultanees s'ecrasaient
  mutuellement. Ce sont maintenant des variables locales.
- **Resynchronisation manuelle.** Le plugin envoyait lui-meme un
  `ObjectDestroyMessage` puis rappelait `SendSpawnMessage` par reflexion.
  `Player.Scale` d'EXILED fait tout cela correctement.

L'anti-lag aleatoire est remplace par un etalement deterministe, et les
callbacks differes sont annules au depart du joueur et au changement de round.
