-- コントロールの操作のマッピングを行います。高度な制御がいるならon_controlを使ってネ
-- map(key_array,button_array,action)
-- 引数にbuttonsとkeysが与えられます。
-- buttons : Votice.XInput.GamepadButtons
-- keys : 未定、現在参照できません map関数の入力は受け入れますが何もしません
map({},{buttons.DPadUp},go_straight)
map({},{buttons.DPadDown},go_back)
map({},{buttons.DPadRight},turn_right)
map({},{buttons.DPadLeft},turn_left)
map({},{buttons.A},grip_down)
map({},{buttons.B},grip_up)
map({},{buttons.LeftThumb},reset_arm)