package one.oneofone.filler

import android.content.Context

/**
 * The prompt block as the ONE/ONE page writes it ("Copy all as one block" / Share):
 *
 *   LYRICS\n...\n\nSTYLES\n...\n\nEXCLUDE STYLES\n...\n\nSLIDERS\n...
 *
 * Parsed by header line, so extra blank lines or a missing section are harmless.
 */
data class Prompt(val lyrics: String, val style: String, val exclude: String, val sliders: String) {
    val isEmpty get() = lyrics.isBlank() && style.isBlank() && exclude.isBlank()

    companion object {
        private val HEADERS = listOf("LYRICS", "STYLES", "EXCLUDE STYLES", "SLIDERS")
        private const val PREFS = "prompt"

        fun parse(block: String): Prompt {
            val sections = HashMap<String, StringBuilder>()
            var current: StringBuilder? = null
            for (line in block.replace("\r\n", "\n").split('\n')) {
                val h = line.trim()
                if (h in HEADERS && (sections[h] == null)) {
                    current = StringBuilder().also { sections[h] = it }
                } else {
                    current?.append(line)?.append('\n')
                }
            }
            fun s(h: String) = sections[h]?.toString()?.trim() ?: ""
            // No headers at all: treat the whole thing as lyrics so a plain share still does something.
            return if (sections.isEmpty()) Prompt(block.trim(), "", "", "") else Prompt(s("LYRICS"), s("STYLES"), s("EXCLUDE STYLES"), s("SLIDERS"))
        }

        fun save(ctx: Context, p: Prompt) {
            ctx.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit()
                .putString("lyrics", p.lyrics).putString("style", p.style)
                .putString("exclude", p.exclude).putString("sliders", p.sliders).apply()
        }

        fun load(ctx: Context): Prompt {
            val sp = ctx.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
            return Prompt(sp.getString("lyrics", "") ?: "", sp.getString("style", "") ?: "", sp.getString("exclude", "") ?: "", sp.getString("sliders", "") ?: "")
        }
    }
}
