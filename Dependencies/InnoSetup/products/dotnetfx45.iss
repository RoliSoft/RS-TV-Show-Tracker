// added based on dotnetfx45.iss - RoliSoft

[CustomMessages]
dotnetfx45_title=.Net Framework 4.5
dotnetfx45_size=3 MB - 197 MB

[Code]
const
	dotnetfx45_url = 'http://download.microsoft.com/download/B/A/4/BA4A7E71-2906-4B2D-A0E1-80CF16844F5F/dotNetFx45_Full_setup.exe';

procedure dotnetfx45();
begin
	if (not netfxinstalled(NetFx45, '')) then
		AddProduct('dotNetFx45_Full_setup.exe',
			'/passive /norestart',
			CustomMessage('dotnetfx45_title'),
			CustomMessage('dotnetfx45_size'),
			dotnetfx45_url,
			false, false);
end;