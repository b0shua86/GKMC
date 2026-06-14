using System.Collections.Generic;
using UnityEngine;

namespace GKMC
{
    /// <summary>A single career easter egg: a glowing emblem on a pedestal you can read up close.</summary>
    public class EasterEgg : MonoBehaviour
    {
        public string title;
        public string sub;   // album · year
        public string desc;  // the lore shown when you interact
    }

    /// <summary>
    /// Scatters easter eggs from Kendrick's whole career across the album's worlds. Each emblem is
    /// model-aware (a Meshy model when present, otherwise a glowing icon). Look at one and press E
    /// to read what it references. Spans Section.80 through GNX and the Super Bowl.
    /// </summary>
    public class EasterEggManager : MonoBehaviour
    {
        public float interactDistance = 5f;
        readonly List<EasterEgg> _eggs = new List<EasterEgg>();
        EasterEgg _focused;

        public static EasterEggManager Create(Transform parent, List<TrackInfo> tracks)
        {
            var go = new GameObject("EasterEggs");
            go.transform.SetParent(parent, false);
            var m = go.AddComponent<EasterEggManager>();
            m.Build(tracks);
            return m;
        }

        struct Def
        {
            public string key, title, sub, desc; public int world; public Vector3 offset; public Color color;
            public Def(string k, string t, string s, string d, int wld, Vector3 off, Color c)
            { key = k; title = t; sub = s; desc = d; world = wld; offset = off; color = c; }
        }

        void Build(List<TrackInfo> tracks)
        {
            var defs = new List<Def>
            {
                new Def("ee_hiiipower", "HiiiPoWeR",
                    "Section.80 · 2011",
                    "Kendrick's debut studio album and the HiiiPoWeR movement — three fingers to the temple for Heart, Honor and Respect. Tales of Tammy and Keisha first introduce the storytelling that defines good kid, m.A.A.d city.",
                    4, new Vector3(-12f, 0f, -2f), new Color(0.4f, 0.9f, 1f)),

                new Def("ee_untitled", "untitled unmastered.",
                    "2016",
                    "Eight untitled loosies from the To Pimp a Butterfly sessions, released raw and unnamed — proof the vault runs deep.",
                    2, new Vector3(12f, 0f, -6f), new Color(0.7f, 0.95f, 0.85f)),

                new Def("ee_pulitzer", "Pulitzer Prize",
                    "DAMN. · 2017–18",
                    "DAMN. won the 2018 Pulitzer Prize for Music — the first ever awarded to a non-classical, non-jazz work. HUMBLE., DNA. and the duality of wickedness vs. weakness.",
                    3, new Vector3(12f, 0f, 16f), new Color(1f, 0.85f, 0.2f)),

                new Def("ee_butterfly", "To Pimp a Butterfly",
                    "2015",
                    "The caterpillar becomes the butterfly. A jazz-funk landmark — King Kunta, Alright (the protest anthem with the helicopters), and the poem that runs through the whole record.",
                    5, new Vector3(11f, 0f, -14f), new Color(1f, 0.7f, 0.1f)),

                new Def("ee_tupac", "Mortal Man",
                    "TPAB · 2Pac",
                    "To Pimp a Butterfly closes with Kendrick interviewing Tupac, stitched from a 1994 recording — passing the torch of Compton's voice across generations.",
                    10, new Vector3(-12f, 0f, -16f), new Color(0.9f, 0.9f, 0.9f)),

                new Def("ee_crown_of_thorns", "Crown of Thorns",
                    "Mr. Morale & the Big Steppers · 2022",
                    "A double album about therapy, generational trauma and accountability. The Heart Part 5 and the crown of thorns Kendrick wore — the cost of being crowned a savior.",
                    10, new Vector3(12f, 0f, 0f), new Color(0.85f, 0.85f, 0.9f)),

                new Def("ee_panther", "Black Panther",
                    "Black Panther: The Album · 2018",
                    "Kendrick and TDE curated and produced the soundtrack to Wakanda — All the Stars with SZA, and a superhero's score for the culture.",
                    12, new Vector3(-13f, 0f, -14f), new Color(0.2f, 0.1f, 0.4f)),

                new Def("ee_pglang", "pgLang",
                    "2020 →",
                    "The multidisciplinary company Kendrick founded with Dave Free after leaving TDE — language beyond words, across music, film and design.",
                    11, new Vector3(11f, 0f, -10f), new Color(0.95f, 0.95f, 0.95f)),

                new Def("ee_gnx", "GNX",
                    "2024",
                    "The surprise album named after the 1987 Buick Grand National GNX — the car he was brought home in as a newborn. squabble up, tv off, luther.",
                    12, new Vector3(12f, 0f, 8f), new Color(0.1f, 0.1f, 0.12f)),

                new Def("ee_uncle_sam", "Super Bowl LIX",
                    "Not Like Us · 2024–25",
                    "The battle that became a movement: Not Like Us swept the 2025 Grammys (Record & Song of the Year), then Kendrick headlined the Super Bowl LIX halftime show — Uncle Sam, SZA and the GNX on the field.",
                    12, new Vector3(-12f, 0f, 14f), new Color(0.8f, 0.2f, 0.2f)),
            };

            foreach (var d in defs)
            {
                var ti = tracks.Find(x => x.number == d.world);
                if (ti == null) continue;
                BuildEgg(d, ti.ZCenter);
            }
        }

        void BuildEgg(Def d, float worldZ)
        {
            var root = new GameObject("EasterEgg_" + d.key);
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(d.offset.x, 0f, worldZ + d.offset.z);

            GKMCUtil.Cyl(root.transform, "Pedestal", new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 0.5f, 1.4f),
                GKMCUtil.Mat(new Color(0.06f, 0.06f, 0.07f), 0.5f, 0.4f));

            // Emblem (model when available, otherwise a glowing icon) — floats and rotates.
            var emblem = ModelLibrary.SpawnOrFallback(d.key, root.transform, new Vector3(0f, 2.2f, 0f), Vector3.zero,
                t => GKMCUtil.Sphere(t, "Icon", Vector3.zero, Vector3.one * 0.9f, GKMCUtil.MatEmissive(d.color, d.color, 2.2f), false));
            var bob = emblem.gameObject.AddComponent<Bobber>(); bob.amplitude = 0.18f; bob.speed = 1.3f;
            var spin = emblem.gameObject.AddComponent<Spinner>(); spin.axis = Vector3.up; spin.degPerSec = 35f;

            GKMCUtil.PointLight(root.transform, "EggGlow", new Vector3(0f, 2.2f, 0f), d.color, 1.8f, 8f);
            // Faces -Z so it reads right to a player walking the tour in along +Z.
            GKMCUtil.Sign(root.transform, "EggLabel", new Vector3(0f, 3.6f, 0f), d.title, d.color, 0.32f, new Vector3(0f, 180f, 0f));

            var sc = root.AddComponent<SphereCollider>();
            sc.center = new Vector3(0f, 2.2f, 0f);
            sc.radius = 1.8f;
            sc.isTrigger = true; // raycast still hits it (queriesHitTriggers), but it won't block walking

            var egg = root.AddComponent<EasterEgg>();
            egg.title = d.title; egg.sub = d.sub; egg.desc = d.desc;
            _eggs.Add(egg);
        }

        void Update()
        {
            var exp = GKMCExperience.Instance;
            if (exp == null || exp.PlayerCamera == null) return;
            var cam = exp.PlayerCamera.transform;

            EasterEgg hitEgg = null;
            if (Physics.Raycast(cam.position, cam.forward, out var hit, interactDistance))
                hitEgg = hit.collider.GetComponentInParent<EasterEgg>();

            _focused = hitEgg;
            if (exp.UI != null) exp.UI.SetEasterEggPrompt(hitEgg);

            if (hitEgg != null && Input.GetKeyDown(KeyCode.E) && exp.UI != null)
                exp.UI.ToggleEasterEggPanel(hitEgg);
        }
    }
}
