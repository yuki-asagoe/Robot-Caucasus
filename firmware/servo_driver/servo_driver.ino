#include <mcp_can.h>
#include <can_communication.h>
#include<Wire.h>
#include<Adafruit_PWMServoDriver.h>

#include "variables.h"

void on_receive_can(uint16_t, const int8_t*, uint8_t);
void set_servo_angle(uint8_t, int);
void set_servo_pulse(uint8_t, double);
void warn(char*);

Adafruit_PWMServoDriver pwm = Adafruit_PWMServoDriver();
unsigned long long last_can_timestamp=0;

void setup(){
  Serial.begin(115200);
  pinMode(STAT_LED1, OUTPUT); // CAN メッセージ受信したら光る
  pinMode(STAT_LED2, OUTPUT); // CAN制御信号が1秒途切れたら光る
  pinMode(SERVO_OFF, OUTPUT);

  pwm.begin();
  pwm.setPWMFreq(PWM_Frequency); //50Hz

  CanCom.begin(CAN_Self_Address, CAN_Speed); 
  CanCom.setReceiveFilter(false);
  CanCom.onReceive(on_receive_can);

  for(int i=0;i<8;i++){
    set_servo_angle(i,Servo_Initial_Angle[i]);
  }

  Serial.println("Ready to Drive - servos on Caucasus");
  Serial.print("My can destination id is\"");
  Serial.print(CAN_Self_Address,DEC);
  Serial.println("\". Git Repository - https://github.com/yuki-asagoe/Robot-Caucasus");
}

void loop(){
  //データ受信を確認し必要ならonReceiveで登録したリスナを呼び出す
  CanCom.tasks();
  unsigned long long now=millis();
  if(now - last_can_timestamp > 1000){ //信号途絶
    digitalWrite(STAT_LED2,HIGH);
  }else{
    digitalWrite(STAT_LED2,LOW);    
  }
  if(now -last_can_timestamp < 100){
    digitalWrite(STAT_LED1,HIGH);
  }else{
    digitalWrite(STAT_LED1,LOW);    
  }
}

void on_receive_can(uint16_t std_id, const int8_t *data, uint8_t len) {
  last_can_timestamp=millis();
  uint8_t msg_type = CanCommunication::getDataTypeFromStdId(std_id);
  uint8_t dest = CanCommunication::getDestFromStdId(std_id);  
  
  Serial.println("Received Data");
  Serial.print("ID-dest:");
  Serial.print(dest,HEX);
  Serial.print(" / ID-msg-type:");
  Serial.print(msg_type,HEX);
  Serial.print(" / Length:");
  Serial.print(len,HEX);
  Serial.print(" / Data:");
  for(int i=0;i<len;i++){
    Serial.print(data[i],HEX);
    Serial.print(" ");
  }
  Serial.println("");
  
  switch(msg_type){
    case CAN_DATA_TYPE_COMMAND:{
      for(uint8_t i=0;i<len;i++){
        uint8_t angle=(uint8_t)data[i];
        if(angle==255){continue;}//値255は無視
        if(angle==255){
          set_servo_angle(i,Servo_Initial_Angle[i]);
        }
        set_servo_angle(i,angle);
      }
    }
    case CAN_DATA_TYPE_EMERGENCY:{
      Serial.println("Emergency Code Detected");
      break;
    }
    Serial.println("");
  }
}

void warn(char* msg){
  Serial.print("Warning: ");
  Serial.print(msg);
  Serial.print(" -by servo drive(id:");
  Serial.print(CAN_Self_Address,DEC);
  Serial.println(")");
}

void set_servo_angle(uint8_t n, int deg) {
    if(deg < 0) deg = 0;
    if(deg > 255) deg = 360;
    double width = Servo_Max_Pulse_Width[n] - Servo_Min_Pulse_Width[n]; //パルス幅の変域の大きさ、ミリ秒
    double pulse = (deg) / 180 * width + Servo_Min_Pulse_Width[n]; //やってることは単なる線形補完のようなもの
    set_servo_pulse(n, pulse);
}

// pulse の時間単位はマイクロ秒
// 周期中に指定したミリ秒の出力を行うように命令する。
void set_servo_pulse(uint8_t n, double pulse) {
  double pulselength = 1000000;   // 1,000,000 マイクロ秒/秒
  pulselength /= PWM_Frequency;   // pwm一周期のマイクロ秒
  pulselength /= 4096;  // 12 bitsで一周期をカウントするとして1ティックあたりのマイクロ秒
  pulse /= pulselength; // 指定されたパルス幅を表現できるティック数の取得
  pwm.setPWM(n, 0, pulse); // ティック数の指定
}