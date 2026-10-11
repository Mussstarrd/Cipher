package one.oneofone.filler

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.Service
import android.content.Context
import android.content.Intent
import android.graphics.Color
import android.graphics.PixelFormat
import android.graphics.Typeface
import android.graphics.drawable.GradientDrawable
import android.os.IBinder
import android.view.Gravity
import android.view.MotionEvent
import android.view.View
import android.view.WindowManager
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.Toast

/**
 * A draggable strip of three chips (LYRICS / STYLES / EXCLUDE) floating over Suno.
 * Tap a chip: if the accessibility service is on, it writes that field straight into the focused
 * Suno text box; otherwise it copies the field to the clipboard so a long-press paste finishes it.
 */
class BubbleService : Service() {
    private lateinit var wm: WindowManager
    private var strip: View? = null

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onCreate() {
        super.onCreate()
        wm = getSystemService(Context.WINDOW_SERVICE) as WindowManager
        startForeground(1, notification())
        show()
    }

    override fun onDestroy() {
        strip?.let { wm.removeView(it) }
        strip = null
        super.onDestroy()
    }

    private fun show() {
        val row = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            setPadding(dp(6), dp(6), dp(6), dp(6))
            background = GradientDrawable().apply { cornerRadius = dp(22).toFloat(); setColor(Color.parseColor("#E6141417")); setStroke(dp(1), Color.parseColor("#2A2A30")) }
        }
        row.addView(chip("1/1", accent = true) { stopSelf() })
        for ((label, key) in listOf("LYRICS" to Field.LYRICS, "STYLES" to Field.STYLE, "EXCL" to Field.EXCLUDE)) {
            row.addView(chip(label) { fill(key) })
        }
        val lp = WindowManager.LayoutParams(
            WindowManager.LayoutParams.WRAP_CONTENT, WindowManager.LayoutParams.WRAP_CONTENT,
            WindowManager.LayoutParams.TYPE_APPLICATION_OVERLAY,
            WindowManager.LayoutParams.FLAG_NOT_FOCUSABLE or WindowManager.LayoutParams.FLAG_LAYOUT_IN_SCREEN,
            PixelFormat.TRANSLUCENT
        ).apply { gravity = Gravity.TOP or Gravity.START; x = dp(12); y = dp(120) }
        // Drag anywhere on the strip; a short press with no movement is a tap on the chip underneath.
        var downX = 0f; var downY = 0f; var startX = 0; var startY = 0
        row.setOnTouchListener { v, e ->
            when (e.actionMasked) {
                MotionEvent.ACTION_DOWN -> { downX = e.rawX; downY = e.rawY; startX = lp.x; startY = lp.y; false }
                MotionEvent.ACTION_MOVE -> {
                    lp.x = startX + (e.rawX - downX).toInt(); lp.y = startY + (e.rawY - downY).toInt()
                    wm.updateViewLayout(v, lp); true
                }
                else -> false
            }
        }
        wm.addView(row, lp)
        strip = row
    }

    private fun fill(field: Field) {
        val p = Prompt.load(this)
        val text = when (field) { Field.LYRICS -> p.lyrics; Field.STYLE -> p.style; Field.EXCLUDE -> p.exclude }
        if (text.isBlank()) { toast("Nothing loaded for ${field.name.lowercase()}. Share a prompt from ONE/ONE first."); return }
        val svc = SunoFillService.instance
        val done = svc?.setFocusedText(text) ?: false
        if (!done) {
            val cm = getSystemService(Context.CLIPBOARD_SERVICE) as android.content.ClipboardManager
            cm.setPrimaryClip(android.content.ClipData.newPlainText("ONE/ONE ${field.name}", text))
            toast(if (svc == null) "Copied. Long-press the Suno box and Paste. (Turn on the accessibility service to skip this.)" else "Copied. Tap into the Suno box first, then tap the chip again.")
        }
    }

    private fun chip(label: String, accent: Boolean = false, onClick: () -> Unit) = TextView(this).apply {
        text = label; typeface = Typeface.MONOSPACE; textSize = 13f
        setTextColor(if (accent) Color.parseColor("#0B0B0D") else Color.parseColor("#F1EFE9"))
        setPadding(dp(12), dp(8), dp(12), dp(8))
        background = GradientDrawable().apply { cornerRadius = dp(18).toFloat(); setColor(if (accent) Color.parseColor("#C8FF3D") else Color.parseColor("#1B1B1F")) }
        layoutParams = LinearLayout.LayoutParams(LinearLayout.LayoutParams.WRAP_CONTENT, LinearLayout.LayoutParams.WRAP_CONTENT).apply { marginEnd = dp(4) }
        setOnClickListener { onClick() }
    }

    private fun notification(): Notification {
        val nm = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        nm.createNotificationChannel(NotificationChannel(CHANNEL, getString(R.string.channel_name), NotificationManager.IMPORTANCE_LOW))
        val open = PendingIntent.getActivity(this, 0, Intent(this, MainActivity::class.java), PendingIntent.FLAG_IMMUTABLE)
        return Notification.Builder(this, CHANNEL)
            .setSmallIcon(R.drawable.ic_slash)
            .setContentTitle(getString(R.string.app_name))
            .setContentText(getString(R.string.notification_text))
            .setContentIntent(open)
            .setOngoing(true)
            .build()
    }

    private fun toast(s: String) = Toast.makeText(this, s, Toast.LENGTH_LONG).show()
    private fun dp(v: Int) = (v * resources.displayMetrics.density).toInt()

    enum class Field { LYRICS, STYLE, EXCLUDE }
    companion object { const val CHANNEL = "bubble" }
}
