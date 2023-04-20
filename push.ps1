# GitHub

git add .
git stage .
git commit -am update
git push

# DockerHub

docker build . -t fortunen/finecontroller
docker tag fortunen/finecontroller fortunen/finecontroller
docker push fortunen/finecontroller
docker pushrm fortunen/finecontroller # https://github.com/christian-korneck/docker-pushrm
