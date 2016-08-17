어셈블리 바인딩 오류 로깅 <small>Assembly Binding Error Logging</small>
==============================

응용 프로그램 런타임에 어셈블리 바인딩 오류 로그를 활성화하여 문제를 추적합니다.

```plaintext
이 대화 상자 대신 JIT(Just-in-time) 디버깅을 호출하는
방법에 대한 자세한 내용은 이 메시지의 뒷부분을 참조하십시오.

************** 예외 텍스트 **************
System.IO.FileNotFoundException: 파일이나 어셈블리 'SomeAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=aaaaaaaaaaaaaaaa' 또는 여기에 종속되어 있는 파일이나 어셈블리 중 하나를 로드할 수 없습니다. 지정된 파일을 찾을 수 없습니다.
파일 이름: 'SomeAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=aaaaaaaaaaaaaaaa'
   위치: MyApplication.MainFrm..ctor()

경고: 어셈블리 바인딩 로깅이 꺼져 있습니다.
어셈블리 바인딩 오류 로깅 기능을 사용하려면 레지스트리 값 [HKLM\Software\Microsoft\Fusion!EnableLog] (DWORD)를 1로 설정하십시오.
참고: 어셈블리 바인딩 오류 로깅 기능을 사용하도록 설정하면 그렇지 않은 경우보다 성능이 약간 떨어집니다.
이 기능을 끄려면 레지스트리 값 [HKLM\Software\Microsoft\Fusion!EnableLog]를 제거하십시오.
```

## 로깅 활성화

레지스트리 편집기를 실행하고 아래 키로 이동 후 값을 추가합니다.

`HKLM\SOFTWARE\Microsoft\Fusion`

```plaintext
EnableLog DWORD 1
ForceLog DWORD 1
LogFailures DWORD 1
LogResourceBinds DWORD 1
LogPath STRING `<PATH TO LOG FILES;e.g.)c:\FusionLog\>`
```

## 응용 프로그램

위 내용 중 레지스트리에 값을 추가하여 로깅을 활성화하고, 레지스트리의 값을 제거하여 로깅을 비활성하는 응용 프로그램입니다.

바로 빌드하려면 아래 조건을 만족해야 합니다.

IDE: Visual Studio 2015
Target Framework: .NET Framework 4.5.2
