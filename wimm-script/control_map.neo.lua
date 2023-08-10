-- コントロールの操作のマッピングを行います。高度な制御がいるならon_controlを使ってネ
-- map(key_array,button_array,action)
-- 引数にbuttonsとkeysが与えられます。
-- buttons : Votice.XInput.GamepadButtons
-- keys : 未定、現在参照できません map関数の入力は受け入れますが何もしません
map({},{buttons.DPadUp},root_up)
map({},{buttons.DPadDown},root_down)
map({},{buttons.X},grip_down)
map({},{buttons.A},grip_up)
map({},{buttons.LeftThumb},reset_arm)