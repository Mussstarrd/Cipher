extends Node
## SongClock (autoload) — the single source of truth for musical time.
##
## All gameplay-critical timing derives from audio playback position, never
## frame delta. The raw audio position is jittery on Android (it only updates
## per mix chunk), so we keep a smoothed clock: advance by delta each frame,
## gently correct toward the raw position, and snap only on large errors
## (seek / loop wrap / stream restart).

## Correction blend applied to the raw-vs-smoothed error each frame.
const CORRECTION_PER_FRAME := 0.05
## Errors beyond this are treated as a seek/wrap and snapped, not smoothed.
const SNAP_ERROR_SEC := 0.1
## How often to re-query the driver's output latency (the call can be costly).
const LATENCY_REFRESH_SEC := 2.0

var bpm := 90.0
var offset_sec := 0.0
var swing_percent := 50.0
var running := false

var _source: AudioStreamPlayer = null
var _smoothed := 0.0
var _raw := 0.0
var _latency := 0.0
var _latency_age := 0.0


## Set the musical parameters of the current track. Safe to call before
## start() so beat<->time math works while a scene is being built.
func configure(p_bpm: float, p_offset_ms: float, p_swing_percent: float) -> void:
	bpm = p_bpm
	offset_sec = p_offset_ms / 1000.0
	swing_percent = p_swing_percent


## Begin tracking the given player. The player should already be playing.
func start(source: AudioStreamPlayer) -> void:
	_source = source
	_latency = AudioServer.get_output_latency()
	_latency_age = 0.0
	_raw = _read_raw()
	_smoothed = _raw
	running = true


func stop() -> void:
	running = false
	_source = null


func _read_raw() -> float:
	if _source == null or not _source.playing:
		return _smoothed
	return _source.get_playback_position() \
		+ AudioServer.get_time_since_last_mix() - _latency


func _process(delta: float) -> void:
	if not running:
		return
	_latency_age += delta
	if _latency_age > LATENCY_REFRESH_SEC:
		_latency = AudioServer.get_output_latency()
		_latency_age = 0.0
	_smoothed += delta
	_raw = _read_raw()
	var err := _raw - _smoothed
	if absf(err) > SNAP_ERROR_SEC:
		_smoothed = _raw
	else:
		_smoothed += err * CORRECTION_PER_FRAME


## Smoothed song time in seconds. Use this for all judgment.
func time() -> float:
	return _smoothed


func raw_time() -> float:
	return _raw


func jitter_ms() -> float:
	return (_raw - _smoothed) * 1000.0


func output_latency_ms() -> float:
	return _latency * 1000.0


func beat_duration() -> float:
	return 60.0 / bpm


func current_beat() -> float:
	return (_smoothed - offset_sec) / beat_duration()


## Linear (unswung) beat position for an arbitrary song time. Used for
## screen-space placement so auto-run speed stays constant.
func linear_beat_at(t: float) -> float:
	return (t - offset_sec) / beat_duration()


## Nominal grid beat -> song time, applying swing to the off-16th of each
## 16th pair (fractional position 0.25 within each half-beat pair).
## swing_percent 50 = straight, 66 = full triplet swing.
func grid_beat_to_time(beat: float) -> float:
	var pair := floorf(beat * 2.0) / 2.0
	var frac := beat - pair
	if absf(frac - 0.25) < 0.001:
		frac = 0.5 * swing_percent / 100.0
	return offset_sec + (pair + frac) * beat_duration()


## Nearest grid slot (at the given beat step, swing-aware) to song time t.
## Returns {beat, time, delta_ms, gap_ms}; gap_ms is the distance to the
## closest neighbouring slot, used to clamp judgment windows so adjacent
## windows can never overlap.
func nearest_grid(t: float, step: float) -> Dictionary:
	var approx := roundf(linear_beat_at(t) / step) * step
	var best_beat := approx
	var best_dt := INF
	for k in range(-2, 3):
		var b := approx + k * step
		var dt := absf(t - grid_beat_to_time(b))
		if dt < best_dt:
			best_dt = dt
			best_beat = b
	var bt := grid_beat_to_time(best_beat)
	var gap := minf(
		absf(bt - grid_beat_to_time(best_beat - step)),
		absf(grid_beat_to_time(best_beat + step) - bt))
	return {
		"beat": best_beat,
		"time": bt,
		"delta_ms": (t - bt) * 1000.0,
		"gap_ms": gap * 1000.0,
	}
