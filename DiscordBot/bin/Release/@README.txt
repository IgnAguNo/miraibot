Download ffmpeg 64bit static from http://ffmpeg.zeranoe.com/builds/
(Direct link: http://ffmpeg.zeranoe.com/builds/win64/static/ffmpeg-20160222-git-45d3af9-win64-static.7z)
Put the extracted directory in your C: drive so C:/ffmpeg/bin/ffmpeg.exe is the correct link to the executable
Add ffmpeg/bin folder to your PATH environment variable: 
- open explorer
- right click 'This PC'
- properties
- advanced system settings
- In the top part, there is a PATH field, add ; to the end and then add your ffmpeg install location /bin/ (C:/ffmpeg/bin/) and save
- (If you're on windows 10 you can just click the add button and insert the install location)
Open command prompt and type ffmpeg to see if you added it correctly: if you did you'll see the version and ~15 lines of info

Find your own user id (we'll call it ownerid) by typing \@YourName in the chat with the slash
Create a new bot account and join the servers you want to use the bot in
Fill in data.credentials.txt file (botemail, botpassword, ownerid) and leave the last lines
Open DiscordBot.exe - You should see it telling you how many text channels it is reading from
To enable the bot in a channel you'll have to use the command `#channel` from your own account in that text channel
For all commands, type `#help` after you've enabled the bot in a text channel