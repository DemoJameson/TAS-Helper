# TAS-Helper

https://gamebanana.com/tools/12383

A Celeste Mod designed to be a tool in TAS making.

# Features:

- In the following, hazards mean vanilla's CrystalStaticSpinner, Lightning and DustStaticSpinner, FrostHelper's CustomSpinner, and VivHelper's CustomSpinner.

- Cycle Hitbox Colors -> basically same as that in CelesteTAS mod, plus a bit modification when hazards are not in view or when spinner freezes.

- Hazard Countdown -> show how many frames later, the Spinner/Lightning/DustBunny will be (un)collidable if condition is satisified.

- Load Range -> including InView Range and NearPlayer Range. Hazards are considered to satisfy some condition in their updates (turn on/off collision etc.) if they are inside/outside corresponding ranges. When using Load Range, will also draw a Load Range Collider of hazards. A hazard is considered to be inside a range, if its Load Range Collider collides with the range. For spinners/dust bunnies, the collider is their center point. For lighting, the Load Range Collider is a rectangle a bit larger than its hitbox.

- Simplified Spinner -> redraw hitbox of Spinner and Dust, also allow you to remove their sprites.

- Pixel Grid -> a pixel grid around player to help you find out the distance easily. Usually to check if player can climbjump/wallbounce.

- Entity Activator Minder -> remind you when a PandorasBox mod's Entity Activator is created.

- Camera Target -> show which direction the camera moves towards. Basically *CameraTarget = Player's position + CameraOffset*, plus CameraTarget should be bounded in room and some other modification, then *CameraPosition = (1-r)\*PreviousCameraPosition + r\*CameraTarget*, where *r = 0.074*. We visualize this by drawing the points Position, PreviousPosition and CameraTarget, and drawing a link from PreviousPosition to CameraTarget.

- Hotkeys -> you can change some of the settings using hotkeys.

- Settings -> Some settings are inherited from CelesteTAS.

# Some details:

- FrostHelper's CustomSpinner may have "no cycle", which means they will turn on/off collidable every frame.

# WIP:

- Support for other hazards.

- Customizable colors.

# Known issues:

- Actual Collide Hitboxes are overridden -> it's actually bad to use actual collide hitboxes when doing a spinner stun, you really need the exact frame the hazard becomes collidable (opaque). So personnally i do not suggest using actual collide hitboxes in this case. Appended hitbox sounds good but current implement relies on opacity to show information. I have no good idea about it so it's set aside.

- Main Switch for Hazard-related is confusing -> will change it if I know how to make a subsubmenu.

- VivHelper spinner isn't fully supported if it's not a regular one -> maybe will add support for them.

- Laggy when there are too many spinners (e.g. Strawberry Jam GrandMaster HeartSide)