#!/usr/bin/env bash

echo "Installing to /usr/bin/youtube-archiver"

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

sudo rm -rf /usr/bin/youtube-archiver

echo "#!/usr/bin/env bash" | sudo tee -a /usr/bin/youtube-archiver > /dev/null
echo "exec dotnet exec $DIR/src/YouTubeArchiver/bin/Debug/netcoreapp3.1/YouTubeArchiver.dll \$*" | sudo tee -a /usr/bin/youtube-archiver > /dev/null
sudo chmod +x /usr/bin/youtube-archiver

echo "Done!"
