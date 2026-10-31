"""Deterministic Kling video-prompt generator.

Template engine over vocab banks keyed by genre family, mood, energy and style preset.
`random.Random(seed)` drives every choice, so the same inputs and seed always give the
same output and a new seed gives a different one. No network, no AI service.
"""

from __future__ import annotations

import math
import random

from . import UserError

PRESETS = {
    "cinematic": "Cinematic",
    "anime": "Anime",
    "vhs": "VHS / lo-fi",
    "3d": "3D render",
    "documentary": "Street documentary",
    "abstract": "Abstract / visualizer",
    "surreal": "Surreal",
}
PRESET_ALIASES = {
    "cinema": "cinematic", "film": "cinematic", "vhs/lo-fi": "vhs", "lo-fi": "vhs",
    "lofi": "vhs", "3d render": "3d", "render": "3d", "street documentary": "documentary",
    "street": "documentary", "doc": "documentary", "abstract/visualizer": "abstract",
    "visualizer": "abstract", "surrealist": "surreal",
}
ASPECTS = ("9:16", "16:9", "1:1")
SHOT_LENS = (5, 10)

# ---- genre families ------------------------------------------------------------

GENRE_FAMILY_KEYWORDS = [
    ("drill", ["drill", "grime", "uk rap"]),
    ("hiphop", ["hip hop", "hip-hop", "hiphop", "trap", "rap", "boom bap", "phonk", "plugg"]),
    ("rnb", ["r&b", "rnb", "r n b", "soul", "neo soul", "neo-soul", "slow jam"]),
    ("lofi", ["lo-fi", "lofi", "lo fi", "chillhop", "jazzhop", "study"]),
    ("electronic", ["edm", "house", "techno", "trance", "dnb", "drum and bass", "drum & bass",
                    "dubstep", "synthwave", "electro", "garage", "breakbeat", "hyperpop",
                    "future bass", "electronic", "dance"]),
    ("pop", ["pop", "k-pop", "kpop", "indie pop", "synth pop", "synthpop"]),
    ("rock", ["rock", "metal", "punk", "grunge", "emo", "shoegaze", "indie rock", "alt"]),
    ("ambient", ["ambient", "chill", "downtempo", "new age", "drone", "meditation", "sleep"]),
    ("acoustic", ["acoustic", "folk", "country", "singer", "songwriter", "bluegrass",
                  "americana", "blues"]),
    ("afro_latin", ["afro", "afrobeats", "amapiano", "reggaeton", "latin", "dancehall",
                    "reggae", "dembow", "bachata", "salsa"]),
    ("orchestral", ["orchestral", "cinematic", "classical", "score", "epic", "trailer",
                    "piano", "neoclassical"]),
]

GENRE_BANKS = {
    "hiphop": {
        "subjects": [
            "a lone figure in an oversized puffer jacket", "a kid on a BMX circling slowly",
            "a group of friends leaning on a parked sedan", "a producer hunched over a drum machine",
            "a hooded silhouette walking toward camera", "a young woman counting steps on a stoop",
            "a dog on a chain watching the street", "two hands rolling a chain between fingers",
            "a rapper mouthing words into a phone camera", "a figure sitting on a rooftop ledge",
        ],
        "settings": [
            "a wet city street under sodium lamps", "a concrete rooftop above a sleeping city",
            "an empty basketball court at midnight", "a corner store lit from the inside",
            "a parking garage with flickering tubes", "a subway platform after the last train",
            "a bedroom studio with cables everywhere", "an underpass covered in tags",
            "a project courtyard with one working light", "a car interior glowing from the dash",
        ],
        "details": [
            "steam rising from a grate", "reflections smeared across wet asphalt",
            "a flickering neon sign half-dead", "rain on the windshield", "cigarette smoke curling",
            "chain-link shadows on the wall", "a bass speaker trembling", "sneakers hanging from a wire",
            "a phone screen lighting a face", "pigeons scattering",
        ],
    },
    "drill": {
        "subjects": [
            "a masked figure standing dead still", "a group in black tracksuits by a stairwell",
            "a rider on an electric bike with no lights", "a single figure under a hood, face hidden",
            "a kid pacing a balcony walkway", "a figure gripping a railing, knuckles white",
            "a shadow crossing a stairwell window", "hands pulling a balaclava into place",
        ],
        "settings": [
            "a council estate at dusk", "a stairwell with one buzzing fluorescent",
            "a tower-block balcony overlooking the city", "a drained canal with graffiti walls",
            "a late bus stop in drizzle", "an off-licence doorway", "a car park roof in the wind",
            "a railway bridge over empty tracks",
        ],
        "details": [
            "drizzle caught in a streetlight", "CCTV camera turning", "tower-block windows lit at random",
            "breath visible in cold air", "a bass hit shaking a puddle", "orange sodium haze",
            "tags scratched into perspex", "a train passing far below",
        ],
    },
    "rnb": {
        "subjects": [
            "a woman in silk brushing hair from her face", "a couple slow-dancing barefoot",
            "a man alone at a candlelit table", "hands tracing a window's condensation",
            "a singer with eyes closed under a single lamp", "a figure stretched across a velvet couch",
            "a woman in the mirror fixing an earring", "two silhouettes almost touching",
        ],
        "settings": [
            "a dim apartment at 2 a.m.", "a hotel room with the curtains half open",
            "a rooftop pool reflecting city lights", "a kitchen lit only by the fridge",
            "a bathroom full of steam and candles", "a night drive through a tunnel",
            "a bedroom with sheer curtains moving", "a jazz bar after closing",
        ],
        "details": [
            "candle flames bending", "water beads on glass", "satin catching light",
            "smoke drifting through a lamp beam", "a record spinning", "rain streaking a window",
            "lipstick on a glass rim", "slow blinking neon from outside",
        ],
    },
    "lofi": {
        "subjects": [
            "a student with headphones at a desk", "a cat asleep on a windowsill",
            "a figure sipping tea by the window", "hands flipping through old vinyl",
            "a girl drawing in a notebook", "a boy napping in a sunbeam", "a bus passenger watching rain",
            "a cassette player on a stack of books",
        ],
        "settings": [
            "a cluttered bedroom at golden hour", "a rainy window seat in a cafe",
            "a tiny Tokyo-style apartment at night", "a rooftop garden with laundry lines",
            "a train carriage at dusk", "a library corner with a desk lamp",
            "a balcony above a quiet street", "a kitchen with a radio playing",
        ],
        "details": [
            "dust motes in a sunbeam", "rain tapping the glass", "steam from a mug",
            "a plant leaning toward the light", "a lamp flickering gently", "a fan turning slowly",
            "pages lifting in a breeze", "city lights blurring outside",
        ],
    },
    "electronic": {
        "subjects": [
            "a dancer moving in strobe freeze-frames", "a crowd of silhouettes with raised hands",
            "a figure in a reflective jacket walking a tunnel", "a DJ's hands over glowing controls",
            "a runner in a neon-lit underpass", "a woman staring into a wall of LEDs",
            "a motorcyclist leaning into a turn", "a lone figure in a laser grid",
        ],
        "settings": [
            "a warehouse rave cut by lasers", "a neon tunnel stretching to infinity",
            "a rain-soaked futuristic street", "a rooftop at blue hour with a skyline",
            "a glass elevator rising through a tower", "an empty arena with a lit stage",
            "a highway at night, lights streaking", "a server room pulsing with LEDs",
        ],
        "details": [
            "strobe flashes freezing motion", "laser beams through haze", "LED panels pulsing to a beat",
            "reflections on wet black floor", "smoke machine clouds", "light trails from traffic",
            "a mirror ball throwing dots", "static glitching across a screen",
        ],
    },
    "pop": {
        "subjects": [
            "a singer spinning in a bright dress", "friends laughing in a convertible",
            "a girl blowing bubblegum at the camera", "a boy dancing alone in a laundromat",
            "a couple running through sprinklers", "a group jumping on a motel bed",
            "a woman in sunglasses on a rooftop", "a figure walking through confetti",
        ],
        "settings": [
            "a pastel suburb at sunset", "a 24-hour diner with chrome booths",
            "a rooftop party with string lights", "a roller rink under a disco ball",
            "a beach boardwalk at dusk", "a bedroom with posters and fairy lights",
            "a city crosswalk at golden hour", "a pink-lit car wash",
        ],
        "details": [
            "confetti drifting", "lens flare across the frame", "bubbles catching light",
            "balloons bumping the ceiling", "neon reflected in sunglasses", "hair whipped by wind",
            "soda fizz close up", "sparklers writing in the dark",
        ],
    },
    "rock": {
        "subjects": [
            "a guitarist silhouetted against a stack of amps", "a crowd surging at a barrier",
            "a figure screaming into a storm", "a drummer blurred in motion",
            "a kid skating an empty pool", "a band in a garage with one bulb",
            "a woman on a motorcycle at a desert crossroads", "a figure smashing a TV",
        ],
        "settings": [
            "a sweaty basement venue", "a desert highway with heat shimmer",
            "a garage with the door half open", "a stadium parking lot before the show",
            "a dive bar lit by beer signs", "an abandoned factory floor",
            "a rooftop in a thunderstorm", "a tour van at a gas station at 3 a.m.",
        ],
        "details": [
            "sweat flying in a strobe", "feedback shaking a cymbal", "dust kicked up in headlights",
            "sparks from a grinder", "beer spray in the air", "a cracked windshield",
            "rain hammering a tin roof", "a flag whipping in the wind",
        ],
    },
    "ambient": {
        "subjects": [
            "a figure standing at the edge of a still lake", "a single boat drifting",
            "a hiker small against a mountain", "hands cupping water", "a bird crossing a huge sky",
            "a woman lying in tall grass", "a monk-like figure walking a ridge", "a jellyfish pulsing",
        ],
        "settings": [
            "a fog-covered lake at dawn", "a glacier valley under soft light",
            "a desert of white dunes", "a forest of tall pines in mist", "a cliff above a slow sea",
            "a salt flat mirroring the sky", "a cave with a shaft of light", "a tundra under aurora",
        ],
        "details": [
            "fog rolling slowly", "ripples spreading", "long shadows stretching", "snow drifting sideways",
            "sunlight breaking through cloud", "grass moving in waves", "mist lifting from water",
            "stars appearing one by one",
        ],
    },
    "acoustic": {
        "subjects": [
            "a guitarist on a porch step", "a woman hanging laundry in a breeze",
            "an old pickup truck on a dirt road", "hands on a weathered fence",
            "a dog running ahead on a trail", "a man watching a train from a field",
            "a couple on a tailgate at sunset", "a child chasing fireflies",
        ],
        "settings": [
            "a farmhouse porch at golden hour", "a wheat field before a storm",
            "a dirt road lined with fence posts", "a small-town main street at dusk",
            "a campfire by a river", "a barn with light through the slats",
            "a mountain cabin in first snow", "a diner counter in morning light",
        ],
        "details": [
            "dust in the sunlight", "wheat swaying", "a screen door swinging", "fireflies blinking",
            "smoke from a chimney", "a rusted sign creaking", "coffee steam in cold air",
            "a creek catching light",
        ],
    },
    "afro_latin": {
        "subjects": [
            "dancers moving hips in sync", "a woman in a bright wrap walking through a market",
            "kids playing football barefoot", "a man drumming on an upturned bucket",
            "a couple dancing close under lights", "a figure on a scooter weaving through traffic",
            "a woman laughing with sea spray in her hair", "a crowd in white at a beach party",
        ],
        "settings": [
            "a street party with string lights", "a beach at sunset with palms",
            "a colorful market at noon", "a rooftop in Lagos or Medellin at dusk",
            "a courtyard with painted walls", "a boardwalk with reggaeton from car speakers",
            "a rainy balcony with plants", "a dance studio with the doors open to the street",
        ],
        "details": [
            "heat shimmer on asphalt", "bright fabrics flowing", "sweat glistening", "palms swaying",
            "spray from a hose arcing", "fruit stacked in color blocks", "lights swinging in a breeze",
            "dust catching sunset light",
        ],
    },
    "orchestral": {
        "subjects": [
            "a lone figure on a cliff edge in a long coat", "a horse galloping through shallow water",
            "a woman walking the aisle of a ruined cathedral", "a soldier-like figure facing a storm",
            "a child holding a lantern in a vast hall", "a conductor's hands in a shaft of light",
            "a ship under full sail against a dark sky", "a figure climbing an endless staircase",
        ],
        "settings": [
            "a ruined cathedral open to the sky", "a cliff above a stormy sea",
            "a snow-covered battlefield at dawn", "a grand hall lit by a thousand candles",
            "a mountain pass above the clouds", "a library with vaulted ceilings",
            "an ancient forest with shafts of light", "a flooded palace",
        ],
        "details": [
            "embers rising", "snow falling in slow motion", "banners moving in wind",
            "dust in cathedral light", "waves crashing on rock", "candle flames flickering together",
            "a cloak billowing", "light breaking through storm clouds",
        ],
    },
    "default": {
        "subjects": [
            "a lone figure walking toward the horizon", "a woman turning to look at the camera",
            "hands reaching into a beam of light", "a figure sitting on a rooftop edge",
            "a dancer moving in slow motion", "a car driving through a tunnel",
            "a person standing in rain without an umbrella", "a silhouette against a huge window",
        ],
        "settings": [
            "a city street at night", "a rooftop at blue hour", "an empty beach at dawn",
            "a long corridor with windows", "a field under a vast sky", "a neon-lit alley",
            "a quiet room with a single window", "a bridge over dark water",
        ],
        "details": [
            "light rain falling", "dust in the air", "reflections on glass", "long shadows",
            "smoke drifting", "wind moving fabric", "lights blurring in the background",
            "a single light source",
        ],
    },
}

# ---- mood banks ---------------------------------------------------------------

MOOD_KEYWORDS = [
    ("dark", ["dark", "menacing", "evil", "sinister", "cold", "eerie", "haunting", "gritty",
              "heavy", "brooding", "ominous", "villain"]),
    ("sad", ["sad", "melanchol", "lonely", "heartbreak", "crying", "pain", "grief", "blue",
             "sorrow", "longing", "bittersweet", "emotional"]),
    ("happy", ["happy", "joy", "uplift", "bright", "fun", "sunny", "playful", "feel good",
               "feel-good", "celebrat", "bouncy", "cheerful", "euphoric"]),
    ("dreamy", ["dream", "ethereal", "float", "hazy", "soft", "airy", "weightless", "spacey",
                "psychedelic", "trippy", "cloud"]),
    ("aggressive", ["aggressive", "angry", "hard", "rage", "violent", "intense", "fierce",
                    "savage", "hype", "banger", "war"]),
    ("romantic", ["romantic", "love", "sensual", "sexy", "intimate", "tender", "passion",
                  "warm", "sweet"]),
    ("nostalgic", ["nostalgic", "nostalgia", "retro", "memory", "vintage", "old", "90s", "80s",
                   "childhood", "summer"]),
    ("mysterious", ["mysterious", "mystery", "noir", "strange", "cryptic", "hypnotic",
                    "suspense", "shadow"]),
    ("calm", ["calm", "chill", "relax", "peaceful", "serene", "gentle", "quiet", "mellow",
              "laid back", "laid-back", "smooth", "meditative"]),
]

MOOD_BANKS = {
    "dark": {
        "words": ["ominous", "cold", "heavy", "brooding", "menacing", "hollow", "tense", "shadowed",
                  "unforgiving", "bleak"],
        "lighting": ["a single hard light from above, deep shadows", "low-key lighting, faces half lost",
                     "sodium orange against pitch black", "cold blue moonlight through blinds",
                     "a flickering fluorescent as the only source", "backlit silhouette, no fill",
                     "headlights cutting through fog", "red emergency light pulsing slowly"],
        "palette": ["near-black with sodium orange", "desaturated blue-grey", "charcoal and dried-blood red",
                    "green-black with one white highlight", "deep navy and rust", "ash grey and oil black",
                    "cold cyan on black", "muted purple shadows"],
    },
    "sad": {
        "words": ["melancholy", "aching", "quiet", "wistful", "lonely", "tender", "heavy-hearted",
                  "bittersweet", "fragile", "distant"],
        "lighting": ["overcast grey daylight through a window", "a dim lamp in an otherwise dark room",
                     "rain-filtered light, soft and blue", "last light of dusk, fading",
                     "a single candle, everything else dark", "cool window light on one side of a face",
                     "streetlight through wet glass", "early morning light, no sun yet"],
        "palette": ["washed-out blue and grey", "faded teal and cream", "muted lavender and slate",
                    "rainy greys with one warm lamp", "cold white and pale blue", "sepia leaning grey",
                    "dusty rose and ash", "midnight blue and dim amber"],
    },
    "happy": {
        "words": ["bright", "buoyant", "playful", "warm", "carefree", "radiant", "giddy", "sunny",
                  "open", "alive"],
        "lighting": ["warm golden-hour sunlight with flare", "bright overcast, soft and even",
                     "midday sun with hard happy shadows", "string lights and sparklers at dusk",
                     "pastel neon bouncing off wet pavement", "sunlight through sprinkler mist",
                     "bright bounce light off white walls", "disco-ball dots sweeping the room"],
        "palette": ["candy pink, sky blue, lemon", "warm orange and turquoise", "coral and mint",
                    "saturated primaries", "peach and gold", "bubblegum and chrome", "lime and hot pink",
                    "cream, tangerine and sea green"],
    },
    "dreamy": {
        "words": ["weightless", "hazy", "floating", "soft-focus", "hypnotic", "glowing", "drifting",
                  "otherworldly", "slow", "shimmering"],
        "lighting": ["soft diffused glow with heavy bloom", "backlight through haze, everything rimmed",
                     "pastel light shifting between pink and blue", "underwater caustics rippling",
                     "moonlight with a soft halo", "lens flare drifting across frame",
                     "bioluminescent glow from below", "light through sheer curtains"],
        "palette": ["lilac, peach and pale blue", "iridescent pastels", "deep violet and soft gold",
                    "milky white and aqua", "rose gold and mist grey", "cyan and magenta haze",
                    "sea-glass green and lavender", "dusk pink and indigo"],
    },
    "aggressive": {
        "words": ["relentless", "sharp", "explosive", "raw", "confrontational", "ferocious", "loud",
                  "charged", "unflinching", "wild"],
        "lighting": ["harsh strobe flashes", "red and white police-style flicker", "hard backlight with lens smear",
                     "fire light flaring from below", "stark white flash with crushed blacks",
                     "sparks as the main light", "high-contrast noon sun", "searchlights sweeping"],
        "palette": ["black, white and blood red", "burnt orange and steel", "acid green on black",
                    "chrome and crimson", "smoke grey and fire", "hard monochrome", "yellow hazard and black",
                    "crushed blacks with hot white"],
    },
    "romantic": {
        "words": ["intimate", "warm", "tender", "slow", "sensual", "close", "golden", "longing",
                  "soft", "velvet"],
        "lighting": ["warm candlelight flickering", "golden lamp light, soft shadows", "city lights bokeh behind",
                     "sunset through a half-open curtain", "neon pink and warm amber mix",
                     "firelight on skin", "soft morning light across sheets", "a single warm practical lamp"],
        "palette": ["burgundy and gold", "rose and amber", "warm skin tones and deep red",
                    "honey and shadow", "wine, cream and brass", "blush pink and midnight", "copper and plum",
                    "sunset orange and violet"],
    },
    "nostalgic": {
        "words": ["faded", "sun-bleached", "wistful", "warm", "familiar", "grainy", "half-remembered",
                  "summery", "tender", "slow"],
        "lighting": ["late-afternoon sun with long flares", "home-video tungsten glow", "overexposed summer light",
                     "porch light at dusk", "TV glow in a dark living room", "sunset through car windows",
                     "fluorescent mall light", "fairground bulbs at night"],
        "palette": ["faded Kodak warm tones", "teal shadows and orange highlights", "VHS-washed pastels",
                    "sun-bleached yellow and denim blue", "sepia and sky", "mint, mustard and brown",
                    "soft magenta cast", "cream and burnt orange"],
    },
    "mysterious": {
        "words": ["enigmatic", "hushed", "shadowed", "hypnotic", "uneasy", "secretive", "slow-burning",
                  "cryptic", "watchful", "noir"],
        "lighting": ["venetian-blind slats of light", "fog with a single backlight", "flashlight beam cutting dark",
                     "green-tinted practicals", "moonlight and one neon sign", "chiaroscuro with hard edges",
                     "light from an unseen source below", "candle in a draft"],
        "palette": ["teal and black", "noir monochrome with one color", "deep green and amber",
                    "ink blue and silver", "fog grey and violet", "bottle green and gold",
                    "near-black with cyan edges", "dusty purple and black"],
    },
    "calm": {
        "words": ["serene", "unhurried", "soft", "steady", "gentle", "spacious", "clear", "balanced",
                  "quiet", "easy"],
        "lighting": ["soft morning light, slightly cool", "even overcast daylight", "warm afternoon sun through leaves",
                     "a reading lamp in a dim room", "dawn light low across water", "gentle window light with sheer curtains",
                     "blue hour, no artificial light", "sunlight on a wooden floor"],
        "palette": ["sage, sand and cream", "pale blue and oat", "warm wood tones and white",
                    "soft grey and seafoam", "clay, moss and linen", "pastel blue and gold", "fog white and pine",
                    "stone and sky"],
    },
    "neutral": {
        "words": ["focused", "moody", "atmospheric", "textured", "cinematic", "grounded", "vivid",
                  "intent", "stylish", "present"],
        "lighting": ["motivated practical lights", "soft key with a hard rim", "mixed warm and cool sources",
                     "golden hour with shadow side to camera", "neon spill from off-screen", "single window as key light",
                     "moonlight with a warm fill", "overhead light with falloff"],
        "palette": ["teal and orange", "muted earth tones", "cool blue and warm amber", "monochrome with one accent",
                    "neon on black", "grey, white and rust", "deep green and gold", "navy and copper"],
    },
}

# ---- energy banks ------------------------------------------------------------

ENERGY_BANKS = {
    "low": {
        "camera": ["static locked-off wide shot", "very slow push-in on a tripod", "slow lateral dolly, almost imperceptible",
                   "gentle handheld drift, breathing", "slow tilt up from the ground", "long lens, static, shallow focus",
                   "slow pull-back revealing the space", "fixed frame, subject moves within it"],
        "pacing": ["slow motion", "real time, unhurried", "long take", "stillness with one small movement",
                   "gentle drift", "held frame", "lingering", "patient"],
    },
    "mid": {
        "camera": ["steady dolly-in", "smooth tracking shot alongside the subject", "slow orbit around the subject",
                   "handheld follow from behind", "crane up revealing the location", "rack focus from foreground to subject",
                   "medium shot, slow push", "wide to medium, steady pull"],
        "pacing": ["natural motion", "steady rhythm", "fluid", "continuous move", "smooth", "measured", "flowing",
                   "deliberate"],
    },
    "high": {
        "camera": ["fast whip pan landing on the subject", "handheld run-and-gun following close", "quick dolly-in with speed ramp",
                   "fast orbit, low angle", "FPV drone dive toward the subject", "snap zoom to close-up",
                   "shaky handheld in the middle of the action", "fast lateral tracking with motion blur"],
        "pacing": ["speed ramp", "fast motion blur", "strobe-cut feel", "kinetic", "frantic", "punchy", "quick cuts implied",
                   "high energy"],
    },
}

# ---- preset banks -------------------------------------------------------------

PRESET_BANKS = {
    "cinematic": {
        "style": ["cinematic film still, anamorphic lens, 35mm grain", "shot on ARRI Alexa, shallow depth of field",
                  "Roger Deakins-style naturalism, wide anamorphic", "moody cinematic, teal-and-orange grade",
                  "epic cinematic, IMAX feel, volumetric light", "neo-noir cinematic, 2.39:1 framing",
                  "A24 indie film look, soft grain, muted grade", "70mm film, deep focus, rich shadows"],
        "negative": ["cartoon", "anime", "text", "watermark", "logo", "low resolution", "distorted hands", "extra limbs",
                     "jittery motion", "oversaturated"],
    },
    "anime": {
        "style": ["90s cel-shaded anime, hand-painted backgrounds", "Makoto Shinkai-style anime, luminous skies",
                  "gritty seinen anime, heavy ink lines", "Studio Ghibli-style anime, soft watercolor backdrops",
                  "cyberpunk anime, neon and rain, detailed linework", "shoujo anime, sparkles and pastel bloom",
                  "retro OVA anime, film grain and limited palette", "modern anime, crisp lines, dramatic lighting"],
        "negative": ["photorealistic", "3D render", "live action", "text", "watermark", "deformed faces", "extra fingers",
                     "blurry lines", "western cartoon"],
    },
    "vhs": {
        "style": ["VHS camcorder footage, tracking lines, timestamp corner", "lo-fi 90s home video, soft and smeared",
                  "degraded VHS, chroma bleed, scan lines", "MiniDV handheld, slight interlacing",
                  "lo-fi analog, overexposed highlights, warm cast", "public-access TV look, low resolution",
                  "found footage, tape dropout artifacts", "camcorder night mode, green-grey noise"],
        "negative": ["clean 4K", "sharp digital", "HDR", "modern color grade", "text overlays other than timestamp",
                     "watermark", "logo", "glossy", "CGI"],
    },
    "3d": {
        "style": ["Octane render, glossy materials, soft studio lighting", "Unreal Engine 5 cinematic, raytraced reflections",
                  "Blender Cycles render, subsurface scattering, clean", "clay-like stylized 3D, soft shadows",
                  "hyperreal 3D product-shot look, chrome and glass", "low-poly 3D with flat shading",
                  "isometric 3D diorama, tilt-shift", "Pixar-style 3D, warm and polished"],
        "negative": ["photograph", "film grain", "hand-drawn", "text", "watermark", "logo", "noise", "flicker",
                     "clipping geometry"],
    },
    "documentary": {
        "style": ["street documentary, handheld, natural light, 50mm", "observational documentary, available light, real textures",
                  "photojournalism style, candid, slightly underexposed", "16mm documentary grain, vérité",
                  "night street photography feel, high ISO", "run-and-gun doc, zoom lens, imperfect framing",
                  "Magnum-photo realism, honest faces, no gloss", "urban documentary, long lens compression"],
        "negative": ["studio lighting", "CGI", "anime", "cartoon", "text", "watermark", "logo", "posed stock-photo look",
                     "oversaturated"],
    },
    "abstract": {
        "style": ["abstract audio visualizer, flowing particles", "macro ink in water, slow swirling color",
                  "generative geometry, pulsing lines on black", "liquid metal ripples, studio reflections",
                  "smoke and light beams, no people", "kaleidoscopic fractal zoom, smooth",
                  "neon wireframe landscape, infinite grid", "paint and oil macro, shifting shapes"],
        "negative": ["people", "faces", "text", "watermark", "logo", "readable letters", "static frame", "clutter",
                     "jarring cuts"],
    },
    "surreal": {
        "style": ["surreal dreamscape, impossible scale, Magritte mood", "floating objects, melting clocks, soft realism",
                  "liminal space, empty mall, uncanny calm", "giant moon too close, cinematic surrealism",
                  "mirror world, gravity sideways, painterly", "Dali-esque desert with stretched shadows",
                  "doors opening onto oceans, photoreal surrealism", "rooms filling with clouds, slow and quiet"],
        "negative": ["text", "watermark", "logo", "horror gore", "extra limbs", "deformed faces", "clutter",
                     "cartoon", "flat lighting"],
    },
}

BASE_NEGATIVE = ["no text", "no watermark", "no logos", "no subtitles"]

MOMENTS = [
    "establishing shot: the whole space before the subject matters",
    "the subject enters or is revealed",
    "a close detail: hands, texture, a small movement",
    "the subject pauses and the music carries",
    "a change: a light switches, weather turns, the subject moves",
    "the widest view, subject small in the frame",
    "the closest view, eyes or hands filling the frame",
    "a reverse angle of the same space",
    "the subject in motion, camera keeping pace",
    "a quiet beat after the action",
    "a reflection or shadow of the subject",
    "a final held frame, the subject still, light fading",
]

VARIANT_LABELS = ["Direct", "Atmospheric", "Intimate", "Wide and lonely", "Kinetic"]


def genre_family(genre: str) -> str:
    g = (genre or "").lower()
    for family, words in GENRE_FAMILY_KEYWORDS:
        if any(w in g for w in words):
            return family
    return "default"


def mood_category(mood: str) -> str:
    m = (mood or "").lower()
    for cat, words in MOOD_KEYWORDS:
        if any(w in m for w in words):
            return cat
    return "neutral"


def normalize_preset(preset: str | None) -> str:
    p = (preset or "cinematic").strip().lower()
    p = PRESET_ALIASES.get(p, p)
    if p not in PRESETS:
        raise UserError(f"'{preset}' is not a style preset. Choose one of: {', '.join(PRESETS)}.")
    return p


def shot_count(clip_len: float, shot_len: int) -> int:
    if shot_len not in SHOT_LENS:
        raise UserError("Kling clip length must be 5 or 10 seconds.")
    try:
        clip_len = float(clip_len)
    except (TypeError, ValueError):
        raise UserError("Clip length must be a number of seconds.")
    if clip_len <= 0:
        raise UserError("Clip length must be more than 0 seconds.")
    return max(1, math.ceil(clip_len / shot_len))


def _cap(text: str) -> str:
    return text[:1].upper() + text[1:] if text else text


def _sample(rng: random.Random, bank: list, n: int) -> list:
    n = min(n, len(bank))
    return rng.sample(bank, n)


def generate(track: dict, preset: str = "cinematic", setting: str = "", aspect: str = "9:16",
             shot_len: int = 5, clip_len: float = 30, n: int = 4, seed: int | None = None,
             shots: int | None = None) -> dict:
    """Return n prompt variants (each with its own shot list) for a track."""
    preset = normalize_preset(preset)
    if aspect not in ASPECTS:
        raise UserError("Aspect ratio must be 9:16, 16:9 or 1:1.")
    try:
        shot_len = int(shot_len)
    except (TypeError, ValueError):
        raise UserError("Kling clip length must be 5 or 10 seconds.")
    try:
        n = int(n)
    except (TypeError, ValueError):
        raise UserError("Number of variants must be a whole number from 3 to 5.")
    if not 3 <= n <= 5:
        raise UserError("Number of variants must be between 3 and 5.")
    default_shots = shot_count(clip_len, shot_len)
    if shots is None or shots == "":
        shots = default_shots
    try:
        shots = int(shots)
    except (TypeError, ValueError):
        raise UserError("Number of shots must be a whole number.")
    if not 1 <= shots <= 24:
        raise UserError("Number of shots must be between 1 and 24.")
    if seed is None or seed == "":
        seed = random.SystemRandom().randrange(1, 10**9)
    try:
        seed = int(seed)
    except (TypeError, ValueError):
        raise UserError("Seed must be a whole number.")

    rng = random.Random(seed)
    family = genre_family(track.get("genre", ""))
    mood_cat = mood_category(track.get("mood", ""))
    energy = (track.get("energy") or "mid").lower()
    if energy not in ENERGY_BANKS:
        energy = "mid"
    g, m, e, p = GENRE_BANKS[family], MOOD_BANKS[mood_cat], ENERGY_BANKS[energy], PRESET_BANKS[preset]
    setting = (setting or "").strip()

    subjects = _sample(rng, g["subjects"], n)
    settings_ = _sample(rng, g["settings"], n)
    lightings = _sample(rng, m["lighting"], n)
    styles = _sample(rng, p["style"], n)
    palettes = _sample(rng, m["palette"], n)
    cameras = _sample(rng, e["camera"], n)

    variants = []
    for i in range(n):
        details = _sample(rng, g["details"], 2)
        mood_words = _sample(rng, m["words"], 3)
        pacing = rng.choice(e["pacing"])
        neg = BASE_NEGATIVE + ["no " + w for w in _sample(rng, p["negative"], 4) if not w.startswith("text")]
        place = setting or settings_[i]
        scene = f"{subjects[i]} in {place}, {details[0]}, {details[1]}"
        v = {
            "label": VARIANT_LABELS[i % len(VARIANT_LABELS)],
            "subject": subjects[i],
            "setting": place,
            "scene": scene,
            "camera": f"{cameras[i]}, {pacing}",
            "lighting": lightings[i],
            "style": styles[i],
            "palette": palettes[i],
            "mood_words": mood_words,
            "negative": ", ".join(dict.fromkeys(neg)),
            "aspect": aspect,
            "duration": shot_len,
        }
        orient = " vertical" if aspect == "9:16" else ""
        v["prompt"] = (
            f"{v['style']}. {_cap(scene)}. {_cap(v['camera'])}. {_cap(v['lighting'])}. "
            f"Mood: {', '.join(mood_words)}. Colors: {v['palette']}. {aspect}{orient}, {shot_len} seconds."
        )
        v["shots"] = _shot_list(rng, v, e, g, shots, shot_len)
        variants.append(v)

    return {
        "seed": seed, "preset": preset, "preset_label": PRESETS[preset], "aspect": aspect,
        "shot_len": shot_len, "clip_len": clip_len, "shots": shots, "default_shots": default_shots,
        "genre_family": family, "mood_category": mood_cat, "energy": energy,
        "setting": setting, "variants": variants,
    }


def _shot_list(rng: random.Random, v: dict, e: dict, g: dict, shots: int, shot_len: int) -> list[dict]:
    """One prompt per shot. Same subject, setting, style and lighting; camera and moment vary."""
    cams = list(e["camera"])
    rng.shuffle(cams)
    moments = list(MOMENTS)
    if shots <= len(moments):
        # keep narrative order: always start with establishing, end with the final frame
        idx = sorted(rng.sample(range(1, len(moments) - 1), max(0, shots - 2)))
        chosen = [moments[0]] + [moments[i] for i in idx] + ([moments[-1]] if shots > 1 else [])
        chosen = chosen[:shots]
    else:
        chosen = [moments[i % len(moments)] for i in range(shots)]
    out = []
    for i in range(shots):
        cam = cams[i % len(cams)]
        detail = rng.choice(g["details"])
        moment = chosen[i]
        prompt = (
            f"{v['style']}. Shot {i + 1} of {shots}: {moment}. {_cap(v['subject'])} in {v['setting']}, {detail}. "
            f"{_cap(cam)}. {_cap(v['lighting'])}. Mood: {', '.join(v['mood_words'])}. Colors: {v['palette']}. "
            f"Same character, wardrobe and location as the other shots. {v['aspect']}, {shot_len} seconds."
        )
        out.append({"n": i + 1, "moment": moment, "camera": cam, "detail": detail,
                    "prompt": prompt, "negative": v["negative"], "duration": shot_len})
    return out


def as_text(result: dict) -> str:
    """Plain-text rendering for the CLI."""
    lines = [f"Seed {result['seed']} | preset {result['preset_label']} | {result['aspect']} | "
             f"{result['shots']} shots x {result['shot_len']} s"]
    for i, v in enumerate(result["variants"], 1):
        lines += ["", f"=== Variant {i}: {v['label']} ===", v["prompt"], f"Negative: {v['negative']}",
                  "--- Shot list ---"]
        for s in v["shots"]:
            lines.append(f"[{s['n']}] {s['prompt']}")
    return "\n".join(lines)
