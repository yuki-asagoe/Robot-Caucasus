slow_mode=false

function go_straight()
	modules.crawlers.right.rotate(1)
	modules.crawlers.left.rotate(1)
end

function go_back()
	modules.crawlers.right.rotate(-1)
	modules.crawlers.left.rotate(-1)
end

function turn_right()
	modules.crawlers.right.rotate(-1)
	modules.crawlers.left.rotate(1)
end

function turn_left()
	modules.crawlers.right.rotate(1)
	modules.crawlers.left.rotate(-1)
end
function grip_down()
	modules.arm.grip.rotate(1)
end
function grip_up()
	modules.arm.grip.rotate(-1)
end
function yaw_right()
	modules.arm.yaw.rotate(1)
end
function yaw_left()
	modules.arm.yaw.rotate(-1)
end
function root_up()
	modules.arm.root.rotate(1)
end
function root_down()
	modules.arm.root.rotate(-1)
end
function roll_right()
	modules.arm.roll.rotate(1)
end
function roll_left()
	modules.arm.roll.rotate(-1)
end
function pitch_up()
	modules.arm.pitch.rotate(1)
end
function pitch_down()
	modules.arm.pitch.rotate(-1)
end
function rotate_belt()
	modules.container.rotate_belt(1)
end
function rotate_belt_back()
	modules.container.rotate_belt(-1)
end
function move_container()
	modules.container.move_container(1)
end
function move_container_back()
	modules.container.move_container(-1)
end
function crawler_up()
	modules.crawlers.updown.rotate(1)
end
function crawler_down()
	modules.crawlers.updown.rotate(-1)
end
function reset_arm()
	modules.other.reboot_arm_servo()
end