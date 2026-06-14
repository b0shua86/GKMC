MODELS — good kid, m.A.A.d city tour
====================================

Drop Meshy-generated (or community) GLB models here, one per prop key:

    car.glb            lowrider.glb        money_tree.glb     palm_tree.glb
    heart.glb          fire_barrel.glb     surveillance_camera.glb
    candle.glb         helicopter_body.glb city_sign.glb
    ee_butterfly.glb   ee_crown_of_thorns.glb  ee_pulitzer.glb
    ee_panther.glb     ee_gnx.glb          ee_hiiipower.glb
    ee_uncle_sam.glb   ee_pglang.glb

Any prop without a model here just renders as a procedural primitive, so the
tour always works.

HOW TO POPULATE
---------------
• In Unity:  GKMC ▸ Open Meshy Generator  (stores your API key in EditorPrefs)
• Or CLI:    export MESHY_API_KEY=...; python3 tools/meshy_generate.py --essential
• Community: grab a model's direct GLB URL from meshy.ai/discover (or your own
  library) and add  "glb_url": "https://..."  to that key in tools/meshy_models.json,
  then run the generator — it downloads instead of generating.

LOADING GLB AT RUNTIME
----------------------
GLB needs the glTFast package + a scripting define:
  1) Window ▸ Package Manager ▸ + ▸ Add package by name ▸ com.unity.cloud.gltfast
  2) GKMC ▸ Enable glTFast (GKMC_GLTFAST)

No glTFast? You can instead place natively-imported models (FBX/OBJ/prefab) at
  Assets/Resources/GKMC_Models/<key>
and they load with no extra package.
