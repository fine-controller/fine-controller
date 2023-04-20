# GitHub

git add .
git stage .
git commit -am update
git push

# DockerHub

docker build . -t fortunen/fine-controller
docker tag fortunen/fine-controller fortunen/fine-controller
docker push fortunen/fine-controller
docker pushrm fortunen/fine-controller # https://github.com/christian-korneck/docker-pushrm
