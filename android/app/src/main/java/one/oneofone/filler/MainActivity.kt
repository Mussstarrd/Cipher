package one.oneofone.filler

import android.accessibilityservice.AccessibilityServiceInfo
import android.app.Activity
import android.content.ClipboardManager
import android.content.Context
import android.content.Intent
import android.graphics.Color
import android.graphics.Typeface
import android.net.Uri
import android.os.Bundle
import android.provider.Settings
import android.view.accessibility.AccessibilityManager
import android.widget.Button
import android.widget.LinearLayout
import android.widget.ScrollView
import android.widget.TextView
import android.widget.Toast

/**
 * Receives the prompt block (via Share from the ONE/ONE page, or from the clipboard), stores it,
 * walks the user through the two permissions, and starts the bubble.
 */
class MainActivity : Activity() {
    private lateinit var status: TextView

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        handleShare(intent)
        val root = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setBackgroundColor(Color.parseColor("#0B0B0D"))
            setPadding(dp(20), dp(28), dp(20), dp(28))
        }
        root.addView(TextView(this).apply {
            text = "ONE/ONE  Filler"
            setTextColor(Color.parseColor("#F1EFE9")); textSize = 22f; typeface = Typeface.MONOSPACE
        })
        status = TextView(this).apply { setTextColor(Color.parseColor("#8D8A83")); textSize = 14f; setPadding(0, dp(8), 0, dp(16)) }
        root.addView(status)
        root.addView(button("1. Allow \"Display over other apps\"") {
            startActivity(Intent(Settings.ACTION_MANAGE_OVERLAY_PERMISSION, Uri.parse("package:$packageName")))
        })
        root.addView(button("2. Turn on the accessibility service") {
            startActivity(Intent(Settings.ACTION_ACCESSIBILITY_SETTINGS))
        })
        root.addView(button("3. Paste prompt block from clipboard") {
            val cm = getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
            val text = cm.primaryClip?.getItemAt(0)?.coerceToText(this)?.toString() ?: ""
            if (text.isBlank()) toast("Clipboard is empty. Tap \"Copy all as one block\" in ONE/ONE first.") else { Prompt.save(this, Prompt.parse(text)); refresh() }
        })
        root.addView(button("4. Show bubble and open Suno") {
            if (!Settings.canDrawOverlays(this)) { toast("Allow \"Display over other apps\" first."); return@button }
            startForegroundService(Intent(this, BubbleService::class.java))
            packageManager.getLaunchIntentForPackage(SunoFillService.SUNO_PACKAGE)?.let { startActivity(it) } ?: toast("Suno app not installed.")
        })
        root.addView(button("Hide bubble") { stopService(Intent(this, BubbleService::class.java)) })
        setContentView(ScrollView(this).apply { addView(root) })
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        handleShare(intent)
        refresh()
    }

    override fun onResume() { super.onResume(); refresh() }

    private fun handleShare(intent: Intent?) {
        if (intent?.action == Intent.ACTION_SEND && intent.type == "text/plain") {
            val text = intent.getStringExtra(Intent.EXTRA_TEXT) ?: return
            Prompt.save(this, Prompt.parse(text))
            toast("Prompt received.")
        }
    }

    private fun refresh() {
        val p = Prompt.load(this)
        val overlay = Settings.canDrawOverlays(this)
        val a11y = isServiceEnabled()
        status.text = buildString {
            append(if (overlay) "Overlay: allowed\n" else "Overlay: not yet allowed\n")
            append(if (a11y) "Accessibility service: on\n" else "Accessibility service: off\n")
            append(if (p.isEmpty) "Prompt: none loaded. Share from ONE/ONE or use step 3." else "Prompt: loaded (lyrics ${p.lyrics.length} chars, styles ${p.style.length}, exclude ${p.exclude.length})\n${p.sliders}")
        }
    }

    private fun isServiceEnabled(): Boolean {
        val am = getSystemService(Context.ACCESSIBILITY_SERVICE) as AccessibilityManager
        return am.getEnabledAccessibilityServiceList(AccessibilityServiceInfo.FEEDBACK_ALL_MASK)
            .any { it.resolveInfo.serviceInfo.packageName == packageName }
    }

    private fun button(label: String, onClick: () -> Unit) = Button(this).apply {
        text = label; isAllCaps = false
        setOnClickListener { onClick() }
        layoutParams = LinearLayout.LayoutParams(LinearLayout.LayoutParams.MATCH_PARENT, LinearLayout.LayoutParams.WRAP_CONTENT).apply { bottomMargin = dp(8) }
    }
    private fun toast(s: String) = Toast.makeText(this, s, Toast.LENGTH_LONG).show()
    private fun dp(v: Int) = (v * resources.displayMetrics.density).toInt()
}
