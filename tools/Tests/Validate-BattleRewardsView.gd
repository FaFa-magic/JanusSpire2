# Run inside an isolated Godot project containing the shipped JanusSpire2 UI
# paths. Uses a rendering-capable display driver (not --headless) for screenshots.
extends SceneTree

var failures = 0
var cases = 0

func check(value: bool, message: String):
	if not value:
		failures += 1
		push_error(message)

func _initialize():
	call_deferred("run_checks")

func run_checks():
	var packed = load("res://JanusSpire2/scenes/ui/janus_battle_rewards.tscn") as PackedScene
	if packed == null:
		quit(1)
		return
	var view = packed.instantiate() as Control
	root.add_child(view)
	var resolutions = [Vector2i(1920,1080), Vector2i(1280,720), Vector2i(2560,1440), Vector2i(2560,1080), Vector2i(1280,1024)]
	var languages = ["zhs","eng","jpn","kor"]
	DirAccess.make_dir_recursive_absolute("res://renders")
	for resolution in resolutions:
		root.size = resolution
		await process_frame
		await process_frame
		for skin in range(7):
			for language in languages:
				var strings = JSON.parse_string(FileAccess.get_file_as_string("res://JanusSpire2/localization/"+language+"/gameplay_ui.json"))
				view.call("configure", skin, strings, null, "", "")
				view.call("finish_entrance")
				var image = view.get_node("Design/Painting/Image") as TextureRect
				check(image.texture != null, "Missing portrait: "+str(skin))
				var background = view.get_node("Design/Painting/Background") as TextureRect
				check(background.texture != null and background.visible, "Missing background: "+str(skin))
				if skin == 2:
					check(background.texture.get_size() == Vector2(1911,2048), "Changing-room reference needs the complete room, not only the transparent mirror props")
				if skin == 5:
					check(background.texture.get_size() == Vector2(2047,924), "Pool skin background must use the restored static scenery, not Live2D")
				check(background.get_index() < image.get_index(), "Background overlaps portrait")
				check(background.z_index == 0 and image.z_index == 0, "Artwork escapes native overlay order")
				var painting = view.get_node("Design/Painting") as Control
				check(image.material == null, "Background heart mask clips/tints the character")
				if skin == 6:
					var heart = background.material as ShaderMaterial
					check(heart != null, "Wedding background heart mask missing")
					if heart != null:
						check(heart.get_shader_parameter("mask_origin_uv").is_equal_approx(-background.position/background.size), "Heart origin moved away from visible background")
						check(heart.get_shader_parameter("mask_size_uv").is_equal_approx(painting.size/background.size), "Heart aspect differs from previous crop")
				else:
					check(background.material == null, "Wedding heart mask leaked to another skin")
				check(image.visible == (skin not in [0,1,3]), "Complete paintings must not double-draw their character")
				if skin == 2:
					check(not view.get_node("Design/Painting").clip_contents, "Static mirror arch is clipped at the portrait box")
					check(background.get_global_rect().position.y >= view.get("header_safe_top")-0.1, "Complete changing room overlaps HUD")
				var zoom = [1.0,1.0,0.96,1.0,0.96,0.88,1.18][skin]
				check(image.size.is_equal_approx(painting.size * zoom), "Existing portrait size changed")
				check(image.position.is_equal_approx((painting.size-image.size)*Vector2(0.5,1.0)), "Existing portrait position changed")
				check(image.stretch_mode == TextureRect.STRETCH_KEEP_ASPECT_CENTERED, "Existing portrait aspect fit changed")
				var frame: Rect2 = load("res://JanusSpire2/images/ui/battle_rewards/background_layout.tres").get_meta(["yanusi","yanusi_3","yanusi_4","yanusi_5","yanusi_6","yanusi_7","yanusi_h"][skin])
				var pixels_to_design = minf(image.size.x/image.texture.get_width(), image.size.y/image.texture.get_height())
				var image_origin = image.position+(image.size-image.texture.get_size()*pixels_to_design)*0.5
				check(background.position.is_equal_approx(image_origin+frame.position*pixels_to_design), "Background origin disconnected from portrait zoom")
				check(background.size.is_equal_approx(frame.size*pixels_to_design), "Background size disconnected from portrait zoom")
				check(painting.get_index() < view.get_node("Design/NamePlate").get_index(), "Artwork covers nameplate")
				check(painting.get_index() < view.get_node("Design/VictoryBanner").get_index(), "Artwork covers victory banner")
				check(view.get("_active_skin") == skin, "Wrong skin selected: "+str(skin))
				check(view.get_node("Design/SkinName").text == strings["JANUS_SPIRE2-BATTLE_REWARDS.previewSkin"+str(skin)], "Wrong skin text")
				check(not view.get_node("Design/PreviewRewards").visible, "Fake rewards visible in live view")
				check(not view.get_node("Design/PreviewProceed").visible, "Fake proceed visible in live view")
				for label_name in ["VictoryTitle","ResultTitle","CharacterName","SkinName"]:
					var label = view.get_node("Design/"+label_name) as Label
					var width = label.get_theme_font("font").get_string_size(label.text, HORIZONTAL_ALIGNMENT_LEFT,-1,label.get_theme_font_size("font_size")).x
					check(width <= label.size.x+1, "Label overflow: "+language+"/"+label_name+"/"+str(skin))
				check(view.get("layout_scale") > 0, "Invalid layout scale")
				check(view.get_node("Design/Grade").get_global_rect().position.y >= view.get("header_safe_top")-1, "Header overlaps HUD")
				for node in view.find_children("*", "Control", true, false):
					check(node.mouse_filter == Control.MOUSE_FILTER_IGNORE, "Decoration blocks input: "+str(node.name))
				if resolution == Vector2i(1920,1080) and language == "zhs":
					view.get_node("Design/PreviewRewards").show()
					view.get_node("Design/PreviewProceed").show()
					await process_frame
					await RenderingServer.frame_post_draw
					root.get_texture().get_image().save_png("res://renders/skin_"+str(skin)+"_zhs.png")
				cases += 1
	view.call("configure", -1, {}, null, "Janus", "")
	check(view.get("_active_skin") == 0, "Invalid skin must fall back")
	view.call("configure", 999, {}, null, "Janus", "")
	check(view.get("_active_skin") == 0, "Invalid upper skin must fall back")
	view.call("prepare_entrance")
	check(is_zero_approx(view.get_node("Design/Grade").modulate.a), "Entrance initial frame not hidden")
	view.call("_process", 3.0)
	check(is_equal_approx(view.get_node("Design/Grade").modulate.a, 1.0), "Entrance did not finish")
	check(not view.is_processing(), "Entrance kept running after completion")
	print("VIEW_TESTS ", cases, " skin/language/resolution cases; failures=", failures)
	view.free()
	quit(0 if failures == 0 else 1)
