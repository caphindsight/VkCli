install:
	echo "#! /usr/bin/env bash" >/usr/bin/vk
	printf "mono $$(pwd)/bin/Release/VkCli.exe" >>/usr/bin/vk
	echo ' ' '"$$@"' >>/usr/bin/vk
	chmod +x /usr/bin/vk

.PHONY: install
