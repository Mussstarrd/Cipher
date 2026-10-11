package one.oneofone.filler

import android.accessibilityservice.AccessibilityService
import android.os.Bundle
import android.view.accessibility.AccessibilityEvent
import android.view.accessibility.AccessibilityNodeInfo

/**
 * Writes text into whichever Suno text box currently has input focus (ACTION_SET_TEXT).
 *
 * Deliberately dumb: it never guesses which box is which by view id or position, because Suno's
 * view ids are unknown and change between releases. The user taps the box, then taps the chip.
 * Scoped to com.suno.android by res/xml/suno_fill_service.xml, so no other app is ever read.
 */
class SunoFillService : AccessibilityService() {

    override fun onServiceConnected() {
        super.onServiceConnected()
        instance = this
    }

    override fun onDestroy() {
        if (instance === this) instance = null
        super.onDestroy()
    }

    override fun onAccessibilityEvent(event: AccessibilityEvent?) { /* nothing to track; we act on demand */ }
    override fun onInterrupt() {}

    /** Returns true when a focused, editable node in Suno accepted the text. */
    fun setFocusedText(text: String): Boolean {
        val root = rootInActiveWindow ?: return false
        if (root.packageName != SUNO_PACKAGE) return false
        val target = root.findFocus(AccessibilityNodeInfo.FOCUS_INPUT)?.takeIf { it.isEditable }
            ?: firstEditable(root)?.takeIf { it.isFocused }
            ?: return false
        val args = Bundle().apply { putCharSequence(AccessibilityNodeInfo.ACTION_ARGUMENT_SET_TEXT_CHARSEQUENCE, text) }
        if (target.performAction(AccessibilityNodeInfo.ACTION_SET_TEXT, args)) return true
        // Some toolkits (Flutter, React Native) only honour paste. Fall back to the clipboard route.
        return false
    }

    private fun firstEditable(node: AccessibilityNodeInfo): AccessibilityNodeInfo? {
        if (node.isEditable) return node
        for (i in 0 until node.childCount) {
            val c = node.getChild(i) ?: continue
            firstEditable(c)?.let { return it }
        }
        return null
    }

    companion object {
        const val SUNO_PACKAGE = "com.suno.android"
        @Volatile var instance: SunoFillService? = null
            private set
    }
}
