extends Control
## Main menu: play / calibrate / author, plus the difficulty (grid
## resolution) selector. Prompts for calibration on first launch.

var _difficulty_btn: Button


func _ready() -> void:
	var bg := ColorRect.new()
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.color = Color(0.06, 0.07, 0.10)
	add_child(bg)

	var center := CenterContainer.new()
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(center)

	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 18)
	center.add_child(box)

	var title := Label.new()
	title.text = "POCKET RUNNER"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", 64)
	box.add_child(title)

	var sub := Label.new()
	sub.text = "tap = jump / bounce · stay on the beat"
	sub.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	sub.add_theme_font_size_override("font_size", 22)
	sub.modulate.a = 0.7
	box.add_child(sub)

	box.add_child(_spacer(20.0))
	box.add_child(_menu_button("PLAY",
		func() -> void: _go("res://scenes/runner.tscn")))
	box.add_child(_menu_button("CALIBRATE",
		func() -> void: _go("res://scenes/calibration.tscn")))
	box.add_child(_menu_button("AUTHOR CHART",
		func() -> void: _go("res://scenes/author.tscn")))

	_difficulty_btn = _menu_button("", _cycle_difficulty)
	_refresh_difficulty_label()
	box.add_child(_difficulty_btn)

	if not Settings.has_calibration:
		_prompt_first_run_calibration()


func _menu_button(text: String, on_pressed: Callable) -> Button:
	var b := Button.new()
	b.text = text
	b.custom_minimum_size = Vector2(440.0, 76.0)
	b.add_theme_font_size_override("font_size", 30)
	b.pressed.connect(on_pressed)
	return b


func _spacer(h: float) -> Control:
	var c := Control.new()
	c.custom_minimum_size = Vector2(0.0, h)
	return c


func _cycle_difficulty() -> void:
	Settings.cycle_difficulty()
	_refresh_difficulty_label()


func _refresh_difficulty_label() -> void:
	var grid := {"easy": "1/4 grid", "medium": "1/8 grid", "hard": "1/16 grid"}
	_difficulty_btn.text = "DIFFICULTY: %s (%s)" \
		% [Settings.difficulty.to_upper(), grid[Settings.difficulty]]


func _prompt_first_run_calibration() -> void:
	var dlg := ConfirmationDialog.new()
	dlg.title = "First launch"
	dlg.dialog_text = "Calibrate your tap timing before playing?\nIt takes about 15 seconds."
	dlg.ok_button_text = "Calibrate"
	dlg.cancel_button_text = "Later"
	add_child(dlg)
	dlg.confirmed.connect(
		func() -> void: _go("res://scenes/calibration.tscn"))
	dlg.popup_centered.call_deferred()


func _go(scene: String) -> void:
	get_tree().change_scene_to_file(scene)
