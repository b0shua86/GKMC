using System.Collections.Generic;
using UnityEngine;

namespace GKMC
{
    /// <summary>One track = one walkable "world". Holds theme text, audio links and palette.</summary>
    [System.Serializable]
    public class TrackInfo
    {
        public int number;
        public string title;
        public string feature;     // e.g. "feat. Jay Rock"
        public string theme;       // short tag shown as a chip
        public string worldDesc;   // longer blurb shown when you enter the world
        public string youtubeUrl;  // link the user can paste / replace
        public string youtubeId;   // optional 11-char id (leave blank to use search url)
        public string audioFile;   // file in StreamingAssets/Audio (e.g. "01.ogg")

        public Color primary;      // ground / structure base
        public Color secondary;    // walls / accents
        public Color accent;       // emissive highlight
        public Color fog;          // atmospheric fog colour
        public Color sky;          // camera background
        public Color ambient;      // ambient light colour
        public float fogDensity;
        public float sun;          // directional light intensity for the world

        public float ZCenter => (number - 1) * AlbumData.WorldLength;
    }

    public static class AlbumData
    {
        public const string Album = "good kid, m.A.A.d city";
        public const string Artist = "Kendrick Lamar";

        public const float WorldLength = 52f;  // depth of each world along +Z
        public const float WorldWidth  = 34f;  // playable width
        public const float WallHeight  = 7f;   // low enough to see the sky/clouds over the verge

        static Color H(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }

        static string SearchUrl(string title)
        {
            string q = ("Kendrick Lamar " + title).Replace(" ", "+");
            return "https://www.youtube.com/results?search_query=" + q;
        }

        public static List<TrackInfo> BuildTracks()
        {
            var t = new List<TrackInfo>();

            void Add(int n, string title, string feat, string theme, string desc,
                     string p, string s, string a, string f, string sk, string amb,
                     float fd, float sun, string ytId = "")
            {
                t.Add(new TrackInfo
                {
                    number = n, title = title, feature = feat, theme = theme, worldDesc = desc,
                    youtubeUrl = SearchUrl(title), youtubeId = ytId,
                    audioFile = (n < 10 ? "0" + n : n.ToString()) + ".ogg",
                    primary = H(p), secondary = H(s), accent = H(a),
                    fog = H(f), sky = H(sk), ambient = H(amb),
                    fogDensity = fd, sun = sun
                });
            }

            Add(1, "Sherane a.k.a Master Splinter's Daughter", "", "temptation",
                "Dusk in Compton. A lone porch light, a parked car, and the pull toward Sherane's. " +
                "Moth-to-flame desire that drives the whole story — and the trap waiting at the end of the block.",
                "#2A2140", "#6B3FA0", "#FF7A3D", "#1A1530", "#2E2348", "#3A2D55", 0.03f, 0.35f);

            Add(2, "Bitch, Don't Kill My Vibe", "", "introspection",
                "\"I am a sinner who's probably gonna sin again.\" A smoke-filled, low-lit headspace — " +
                "candles drifting in haze, the calm before the chaos.",
                "#11302B", "#1F5F54", "#6FE0C8", "#0C2420", "#143A33", "#1A463E", 0.045f, 0.4f);

            Add(3, "Backseat Freestyle", "", "ego & ambition",
                "\"All my life I want money and power.\" Pure teenage bravado — a gold hall of mirrors, " +
                "towering ego, chains and dreams of the Eiffel Tower.",
                "#1A1408", "#3D2F0A", "#FFC400", "#120E04", "#241A06", "#2A2008", 0.025f, 0.5f);

            Add(4, "The Art of Peer Pressure", "", "the homies",
                "Riding with the homies, doing what you'd never do alone. A restless night route lined " +
                "with silhouettes pulling you forward toward a house that isn't yours.",
                "#161A22", "#2E3647", "#9AA7FF", "#0E1119", "#1A2030", "#20283A", 0.035f, 0.35f);

            Add(5, "Money Trees", "feat. Jay Rock", "dreams of escape",
                "\"It go Halle Berry or hallelujah.\" Sun-warmed backyard nostalgia where the trees grow " +
                "money — the imagined perfect place just out of reach.",
                "#1E3A1A", "#3F6B2A", "#F2D04B", "#294A24", "#5C7A3A", "#4A6B33", 0.02f, 1.0f);

            Add(6, "Poetic Justice", "feat. Drake", "love",
                "A Janet-Jackson-soft sunset. Rose petals, slow movement and warmth — the tender " +
                "counterweight to the violence around it.",
                "#3A1530", "#7A2E63", "#FF8FB8", "#2A1024", "#5A2148", "#4A1C3A", 0.03f, 0.7f);

            Add(7, "good kid", "", "the system",
                "A good kid in a mad city. Cold institutional grey, chain-link, surveillance cameras and " +
                "searchlights from the choppers above. The mass control closing in.",
                "#1B2026", "#38424C", "#7FB2FF", "#141A20", "#232C34", "#28323C", 0.04f, 0.3f);

            Add(8, "m.A.A.d city", "feat. MC Eiht", "chaos & violence",
                "\"Compton, U.S.A. made me an Angel on Angel Dust.\" Fire-barrel red, sirens, broken glass " +
                "and graffiti. The most dangerous block on the tour — choppers low overhead.",
                "#220606", "#5C0F0F", "#FF3B1F", "#1A0404", "#340808", "#3A0A0A", 0.05f, 0.45f);

            Add(9, "Swimming Pools (Drank)", "", "addiction",
                "Drowning in liquor. A vast blue pool you sink into, cups floating overhead and the muffled, " +
                "underwater pull of peer pressure.",
                "#07203A", "#0E4A7A", "#3FB6FF", "#051A30", "#0A3358", "#0C3A60", 0.05f, 0.5f);

            Add(10, "Sing About Me, I'm Dying of Thirst", "", "mortality",
                "Two movements: a candlelit memorial to the people who want him to sing about them, fading " +
                "into a baptismal pool — thirst for meaning, and the prayer that answers it.",
                "#1F1606", "#5A3A12", "#FFB347", "#160F05", "#2A1C0A", "#2E2010", 0.04f, 0.4f);

            Add(11, "Real", "", "self-love",
                "\"I'm real, I'm really real.\" Mirrors of truth and a glowing heart at the centre — the " +
                "realization that love of self is the only thing that's real.",
                "#2A1414", "#6E2B2B", "#FF6B6B", "#201010", "#3A1E1E", "#3A2020", 0.03f, 0.65f);

            Add(12, "Compton", "feat. Dr. Dre", "triumph",
                "Bright California daylight. Palm trees, a lowrider, the Compton sign and a victory lap " +
                "through the city that made him — and everything his career became after.",
                "#244A6B", "#3E7FB0", "#FFD23F", "#6FA8D8", "#7FB5E6", "#9CC4E0", 0.012f, 1.2f);

            return t;
        }
    }
}
