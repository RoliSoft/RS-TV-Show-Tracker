[Files]
Source: "Dependencies\InnoSetup\iswin7\iswin7.dll"; DestDir: {tmp}; Flags: dontcopy

[Code]
procedure iswin7_add_glass(Handle:HWND; Left, Top, Right, Bottom : Integer; GDIPLoadMode: boolean);
external 'iswin7_add_glass@files:iswin7.dll stdcall';

procedure iswin7_add_button(Handle:HWND);
external 'iswin7_add_button@files:iswin7.dll stdcall';

procedure iswin7_free;
external 'iswin7_free@files:iswin7.dll stdcall';

