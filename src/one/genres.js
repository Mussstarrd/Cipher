/* ONE OF ONE — genre packs. Each pack overrides whole sections of the base
 * grammar (src/one/grammar.js). The base is the syncopated experimental
 * hip hop engine; the others are spin-offs with their own instruments,
 * drums, groove, structure and exclusions. */
"use strict";

const OneGenres = {
  hiphop: {
    id: "hiphop",
    label: "Syncopated hip hop",
    blurb: "Twitchy, minimal, one-of-one instrumentals. Beat switch or hypnotic loop.",
    grammar: {},
  },
};

if (typeof module !== "undefined") module.exports = OneGenres;
