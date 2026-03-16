# LittleNPCs

Mod for [Stardew Valley](http://stardewvalley.net/) which turns your children into little NPCs. Requires [ContentPatcher](https://www.nexusmods.com/stardewvalley/mods/1915).

**Create a content pack**

Replacement NPCs for your children must be provided by content packs. See [NPC data](https://stardewvalleywiki.com/Modding:NPC_data). Note that NPCDispositions must not be provided by your content pack, these are generated and handled internally by the mod.

Starting from version 1.1, the mod provides 2 ContentPatcher tokens:

* Candidus42.LittleNPCs/FirstLittleNPC
* Candidus42.LittleNPCs/SecondLittleNPC

Starting from version 2.2, the mod provides 2 more ContentPatcher tokens:

* Candidus42.LittleNPCs/ThirdLittleNPC
* Candidus42.LittleNPCs/FourthLittleNPC

Every token supports these arguments:

* Name
* DisplayName
* Gender
* BirthSeason
* BirthDay
* Age

**The 1.0 tokens had to be removed in version 2.2 due to hard-to-debug token update conflicts. Sorry!**

Only one argument can be given at a time. Token arguments are passed after a colon. For example, if you want the internal name of FirstLittleNPC, use
`{{Candidus42.LittleNPCs/FirstLittleNPC: Name}}`

In your content pack, use these tokens instead of hard-coded names and genders.

**Convert a ChildToNPC content pack**

Content packs for the unmaintained [ChildToNPC](https://www.nexusmods.com/stardewvalley/mods/4568) mod are supposed to be easily convertible, even though some tokens are not provided.

Remove `Data/NPCDispositions` from your content pack first. For replacing tokens see the following table:


| Child2NPC token     | LittleNPCs 1.0 token       | LittleNPCs 1.1 token and argument| LittleNPCs 2.2 token and argument| Notes                                                        |
|:--------------------|:---------------------------|:---------------------------------|:---------------------------------|:-------------------------------------------------------------|
| FirstChildName      | FirstLittleNPCName         | FirstLittleNPC: Name             | FirstLittleNPC: Name             | Internal asset name, not suitable for dialogue.              |
|                     | FirstLittleNPCDisplayName  | FirstLittleNPC: DisplayName      | FirstLittleNPC: DisplayName      | Name to show in dialogue.                                    |
| FirstChildBirthday  |                            |                                  |                                  | Not needed anymore. Formerly used to provide NPCDispositions.|
|                     |                            | FirstLittleNPC: BirthSeason      | FirstLittleNPC: BirthSeason      | Season of birth: spring, summer, fall or winter.             |
|                     |                            | FirstLittleNPC: BirthDay         | FirstLittleNPC: BirthDay         | Day of birth: 1 to 28.                                       |
|                     |                            | FirstLittleNPC: Age              | FirstLittleNPC: Age              | Age of a LittleNPC in years.                                 |
| FirstChildBed       | n/a                        | n/a                              | n/a                              | Not needed anymore. Formerly used to provide NPCDispositions.|
| FirstChildGender    | FirstLittleNPCGender       | FirstLittleNPC: Gender           | FirstLittleNPC: Gender           |                                                              |
| FirstChildParent    | n/a                        | n/a                              | n/a                              | Use the standard CP token {{spouse}} instead.                |
|                     |                            |                                  |                                  |                                                              |
| SecondChildName     | SecondLittleNPCName        | SecondLittleNPC: Name            | SecondLittleNPC: Name            | Internal asset name, not suitable for dialogue.              |
|                     | SecondLittleNPCDisplayName | SecondLittleNPC: DisplayName     | SecondLittleNPC: DisplayName     | Name to show in dialogue.                                    |
| SecondChildBirthday |                            |                                  |                                  | Not needed anymore. Formerly used to provide NPCDispositions.|
|                     |                            | SecondLittleNPC: BirthSeason     | SecondLittleNPC: BirthSeason     | Season of birth: spring, summer, fall or winter.             |
|                     |                            | SecondLittleNPC: BirthDay        | SecondLittleNPC: BirthDay        | Day of birth: 1 to 28.                                       |
|                     |                            | SecondLittleNPC: Age             | SecondLittleNPC: Age             | Age of a LittleNPC in years.                                 |
| SecondChildBed      | n/a                        | n/a                              | n/a                              | Not needed anymore. Formerly used to provide NPCDispositions.|
| SecondChildGender   | SecondLittleNPCGender      | SecondLittleNPC: Gender          | SecondLittleNPC: Gender          |                                                              |
| SecondChildParent   | n/a                        | n/a                              | n/a                              | Use the standard CP token {{spouse}} instead.                |
|                     |                            |                                  |                                  |                                                              |
| ThirdChildName      | n/a                        | n/a                              | ThirdLittleNPC: Name             | Internal asset name, not suitable for dialogue.              |
|                     |                            |                                  | ThirdLittleNPC: DisplayName      | Name to show in dialogue.                                    |
| ThirdChildBirthday  | n/a                        | n/a                              |                                  | Not needed anymore. Formerly used to provide NPCDispositions.|
|                     |                            |                                  | ThirdLittleNPC: BirthSeason      | Season of birth: spring, summer, fall or winter.             |
|                     |                            |                                  | ThirdLittleNPC: BirthDay         | Day of birth: 1 to 28.                                       |
|                     |                            |                                  | ThirdLittleNPC: Age              | Age of a LittleNPC in years.                                 |
| ThirdChildBed       | n/a                        | n/a                              | n/a                              | Not needed anymore. Formerly used to provide NPCDispositions.|
| ThirdChildGender    | n/a                        | n/a                              | ThirdLittleNPC: Gender           |                                                              |
| ThirdChildParent    | n/a                        | n/a                              | n/a                              | Use the standard CP token {{spouse}} instead.                |
|                     |                            |                                  |                                  |                                                              |
| FourthChildName     | n/a                        | n/a                              | FourthLittleNPC: Name            | Internal asset name, not suitable for dialogue.              |
|                     |                            |                                  | FourthLittleNPC: DisplayName     | Name to show in dialogue.                                    |
| FourthChildBirthday | n/a                        | n/a                              |                                  | Not needed anymore. Formerly used to provide NPCDispositions.|
|                     |                            |                                  | FourthLittleNPC: BirthSeason     | Season of birth: spring, summer, fall or winter.             |
|                     |                            |                                  | FourthLittleNPC: BirthDay        | Day of birth: 1 to 28.                                       |
|                     |                            |                                  | FourthLittleNPC: Age             | Age of a LittleNPC in years.                                 |
| FourthChildBed      | n/a                        | n/a                              | n/a                              | Not needed anymore. Formerly used to provide NPCDispositions.|
| FourthChildGender   | n/a                        | n/a                              | FourthLittleNPC: Gender          |                                                              |
| FourthChildParent   | n/a                        | n/a                              | n/a                              | Use the standard CP token {{spouse}} instead.                |
| NumberTotalChildren | n/a                        | n/a                              | n/a                              | Not needed anymore. Number of children is handled internally.|

**Config options**

* AgeInDaysWhenChildrenBecomeLittleNPCs: The age in days when a child is replaced by a LittleNPC. Default is 83 days.
* DoChildrenRunAroundInTheHouse: If true, children run around in the house every hour unless they have a schedule.
* DoChildrenHaveCurfew: If true, children will head home at curfew time.
* CurfewTime: The time of curfew when DoChildrenHaveCurfew is true. Default is 1900 (7PM).
* DoChildrenVisitVolcanoIsland: Children visit Volcano Island by chance. Default is false.
* MaximumNumberOfChildren: Maximum number of children. Default is 4.
* MaximumNumberOfLittleNPCs: Maximum number of children that become LittleNPCs. Default is 4.
