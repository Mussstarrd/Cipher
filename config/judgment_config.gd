class_name JudgmentConfig
extends Resource
## Tuning values live here (res://config/game_config.tres), not in code.
##
## Judgment windows are +/- milliseconds around a grid slot. The outer
## window keeps you alive (bounce, no chain credit); the inner window feeds
## the chain. Effective widths are clamped at runtime against the actual
## gap between neighbouring grid slots so adjacent windows never overlap,
## which is what scales them with grid density.

@export var easy_outer_ms := 160.0
@export var easy_inner_ms := 80.0
@export var medium_outer_ms := 130.0
@export var medium_inner_ms := 60.0
@export var hard_outer_ms := 90.0
@export var hard_inner_ms := 45.0

## Spatial gate: max distance of the player's feet from the enemy head zone.
@export var spatial_threshold_px := 64.0
## Bounce leniency after an arc lands (keeps 1-beat chains playable).
@export var landing_grace_ms := 90.0

## Auto-run speed is derived from BPM via this constant, so enemies spaced
## N beats apart are always spaced consistently on screen.
@export var px_per_beat := 220.0
@export var jump_height_px := 150.0
## A jump is a fixed-duration arc lasting an exact beat subdivision.
@export var jump_duration_beats := 1.0

@export var chain_thresholds: Array[int] = [4, 8, 16]
@export var start_coins := 10
@export var coins_dropped_on_miss := 5
@export var coin_recollect_seconds := 2.5
@export var drums_unmute_beats := 0.5


func outer_ms(difficulty: String) -> float:
	if difficulty == "easy":
		return easy_outer_ms
	if difficulty == "medium":
		return medium_outer_ms
	return hard_outer_ms


func inner_ms(difficulty: String) -> float:
	if difficulty == "easy":
		return easy_inner_ms
	if difficulty == "medium":
		return medium_inner_ms
	return hard_inner_ms
