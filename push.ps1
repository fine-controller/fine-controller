# GitHub

git add .
git stage .
git commit -am update
git push

# DockerHub

docker build . -t fortunen/fineoperator
docker tag fortunen/fineoperator fortunen/fineoperator
docker push fortunen/fineoperator
docker pushrm fortunen/fineoperator # https://github.com/christian-korneck/docker-pushrm
