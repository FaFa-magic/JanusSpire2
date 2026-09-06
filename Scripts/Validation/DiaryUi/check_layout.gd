extends SceneTree

func _initialize():
    call_deferred("check_layout")

func check_layout():
    var packed = load("res://JanusSpire2/scenes/screens/diary_card_pile_screen.tscn") as PackedScene
    assert(packed != null)
    var screen = packed.instantiate() as Control
    assert(not screen.has_node("ViewUpgrades"))
    root.add_child(screen)
    for viewport_size in [Vector2i(1920, 1080), Vector2i(2560, 1440), Vector2i(1280, 720)]:
        root.size = viewport_size
        await process_frame
        await process_frame
        var all_slots = []
        for page_name in ["LeftPage", "RightPage"]:
            var page = screen.get_node(page_name) as Control
            var grid = page.get_node("Margin/CardSlots") as GridContainer
            assert(grid.columns == 3)
            assert(grid.get_child_count() == 9)
            assert(page.size.x > 0 and page.size.y > 0)
            for slot in grid.get_children():
                assert(slot.size.x > 0 and slot.size.y > 0)
                assert(slot.has_node("CardAnchor"))
                assert(page.get_global_rect().encloses(slot.get_global_rect()))
                var factor = min(slot.size.x / 315.0, slot.size.y / 355.0)
                # Vanilla resting card dimensions: 300x422 at holder scale 0.8.
                var card_size = Vector2(300, 422) * 0.8 * factor
                assert(card_size.x <= slot.size.x and card_size.y <= slot.size.y)
                for other in all_slots:
                    assert(not slot.get_global_rect().intersects(other.get_global_rect()))
                all_slots.append(slot)
        assert(all_slots.size() == 18)
        assert(screen.get_node("LeftPage").get_global_rect().end.x < screen.get_node("RightPage").get_global_rect().position.x)
        print("PASS 18 distinct slots, left 3x3 / right 3x3, cards fit: ", viewport_size,
            " cell=", all_slots[0].size)
    assert(screen.get_node("Background").texture != null)
    var left_icon = screen.get_node("PreviousPage/TextureRect")
    var right_icon = screen.get_node("NextPage/TextureRect")
    assert(left_icon.texture != null and right_icon.texture != null)
    assert(left_icon.material != right_icon.material)
    print("PASS background, local font/arrow resources and independent arrow materials")
    screen.free()
    quit()
