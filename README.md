# TAS-Helper

https://gamebanana.com/tools/12383

This mod is based on [CelesteTAS](https://github.com/EverestAPI/CelesteTAS-EverestInterop) and aims to provide some extra convenience for making TASes.

# Features:

- Check the menu in game! The menu should contain enough descriptions about TAS Helper features.

- In the following, hazards mean vanilla's CrystalStaticSpinner, Lightning and DustStaticSpinner, FrostHelper's CustomSpinner/AttachedLightning, VivHelper's CustomSpinner, and ChronoHelper's ShatterSpinner/DarkLightning.

- Cycle Hitbox Colors -> basically same as that in CelesteTAS mod, plus a bit modification when hazards are not in view or when spinner freezes.

- Hazard Countdown -> show how many frames later, the Spinner/Lightning/DustBunny will be (un)collidable if condition is satisified. Being gray or not indicates the hazard's collidability.

- Load Range -> including InView Range and NearPlayer Range. Hazards are considered to satisfy some condition in their updates (turn on/off collision etc.) if they are inside/outside corresponding ranges. When using Load Range, will also draw a Load Range Collider of hazards. A hazard is considered to be inside a range, if its Load Range Collider collides with the range. For spinners/dust bunnies, the collider is their center point. For lighting, the Load Range Collider is a rectangle a bit larger than its hitbox. The purple box is Actual Near Player Range, which appears when player's position changed during different NearPlayer checks, just like actual collide hitboxes.

- Simplified Spinner -> redraw hitbox of Spinner and Dust, also allow you to remove their sprites.

- Predictor -> predict the future track of your tas file in real time, no longer need to run tas frequently!

- Pixel Grid -> a pixel grid around player to help you find out the distance easily. Usually to check if player can climbjump/wallbounce.

- Entity Activator Reminder -> remind you when a PandorasBox mod's Entity Activator is created.

- Camera Target -> show which direction the camera moves towards. Basically *CameraTarget = Player's position + CameraOffset*, plus CameraTarget should be bounded in room and some other modification, then *CameraPosition = (1-r)\*PreviousCameraPosition + r\*CameraTarget*, where *r = 0.074*. We visualize this by drawing the points Position, PreviousPosition and CameraTarget, and drawing a link from PreviousPosition to CameraTarget.

- CustomInfoHelper -> provide some fields / properties which are not easy to compute in CelesteTAS's CustomInfo. Check [CustomInfoHelper](Source/Gameplay/CustomInfoHelper.cs).

- Order-of-Operation Stepping -> just like frame advance, but in a subframe scale, thus visualize order of operations in a frame. The bottom-left message indicates the next action (if there's no "begin/end" postfix) / current action (if there is) of the game engine.

- Allow opening console in TAS.

- Scrollable Console History Log -> Besides holding ctrl + up/down (provided by Everest), you can now use MouseWheel/PageUp/PageDown to scroll over the history logs. Press Ctrl+PageUp/Down to scroll to top/bottom.

- Hotkeys -> you can change some of the settings using hotkeys.

- Main Switch hotkey -> Settings are memorized in a way that, ActualSettings = MainSwitch state && MemorizedSettings (if both sides are boolean. Similar for other types). The Main Switch hotkey just modifies MainSwitch state, and will not modify MemorizedSettings. Editing settings in menu or using other hotkeys will modify MemorizedSettings.

- Auto Watch Entity -> (the name comes from [XMinty77](https://github.com/EverestAPI/CelesteTAS-EverestInterop/pull/32)) Mainly 3 kinds of auto watch, show timer / show speed / show other static info. If the entity is moving around slowly, then we can also show its offset from its origin. Notations: "~" means it's waiting until some condition is satisified (e.g. Player.DummyWalkTo needs player's position to be close enough to the target), and "\[num\]\~" means we need to either wait until the timer expires, or when some condition is satisfied. (e.g. FallingBlock will wait some extra time before falling, until we are no longer climbing it or the timer expires.)(the interpretation of the timer may vary as the entity change. The basic idea is that, when the timer becomes 0, then the next frame the entity will change its "state". But it may rely on OoO or how i've implemented, to say if next frame updates in this state but ends in another state, or just updates in another state.) Trigger is a bit special, its text will be hidden when player is near, unless you've manually clicked on the trigger to watch it. If a "trigger" has a "*" suffix, then it is techinically not a trigger. (e.g. RespawnTargetTrigger)

- Add some commands -> Currently we have, spinner_freeze cmd, nearest_timeactive cmd, setting-related cmd, OoO config cmd. Check [Commands](Docs/Commands.md).

- ...

# Feature Request:

  If you have feature requests related to TAS, you can ping/DM me @Lozen#0956 on Celeste discord server. Please describe your feature request as detailed as possible. However, there is no guarantee that the final result will be same as what you've demanded.

  When a feature is useful and standard enough to become a part of CelesteTAS, this feature will first be merged into TAS Helper (so you can get it at first time), and a pull request/an issue on this feature will be submitted to CelesteTAS simultaneously.

# Some details:

- The number shown in AutoWatch may change dramatically when Engine.TimeRate changes, e.g. when you bounce on an attacking seeker.

- FrostHelper's CustomSpinner may have "no cycle", which means they will turn on/off collidable every frame.

- BrokemiaHelper's CassetteSpinner, is considered as "no cycle", since its collidablity is completely determined by cassette music. However, its visibility do have a 15f cycle (useless, it can't interact with collidablity).

# Known issues:

- AutoWatchEntity sometimes generates little black rect on screen -> don't know why. Seems related with AutoWatchEntity.Trigger.

- AutoWatchEntity sometimes doesn't work -> it's possible if there's some mod which add some hooks and interfere AutoWatchEntity, please tell me if that happens.

- There will be some offset between HiresRenderer and Gameplay contents when we use ExtendedVariant.ZoomLevel. This also applies to CelesteTAS.CenterCamera when we zoom out. -> maybe will fix this later.

- VivHelper spinner isn't fully supported if its hitbox is not prestored -> maybe will add support for them.

- Laggy when there are too many spinners (e.g. Strawberry Jam GrandMaster HeartSide) -> Partially solved in v1.4.7.

- Predictor can't handle commands like StunPause Simulate (StunPause Input is ok), SetCommands, InvokeCommands and so on. -> Currently don't plan to support them. Tell me if you need this feature.

- Celeste TAS hotkeys randomly work improperly -> Not sure if it's caused by TAS Helper.

- Reverse Frame Advance sometimes make camera wrong -> Don't know why.

- ~~Use SRT save, then reload asset, then SRT load. This causes crash -> I guess it's a general issue and only happens for mod developers, so just ignore it.~~