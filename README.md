# VkCli
Vk CLI Client. Provides basic functionality such as receiving and sending text messages from/to friends and public chats.

## Installation
1. Download mono and nuget: `sudo apt-get install mono-complete nuget`
2. Clone this repository
3. Download the required dependencies: `nuget restore`
4. Build the solution: `xbuild /p:Configuration=Release`
5. Install VkCli: `sudo make install`

## Logging in
Run `vk login` and follow the instructions. You will have to allow access to your profile in the web interface.

## How people are identified
Each contact is identified by its numeric _id_.
It is convinient to use text abbreviations for frequent contacts.
This can be achieved by `vk abbr {abbr} {id}`.
Use `vk abbrs` to see the list of your abbreviations.

## Frequently used functionality
* Run `vk --help` to see a detailed description of available commands
* Run `vk check` to see previews of pending dialogs. You will see nothing in case nobody is trying to reach you on VK.
* Run `vk friends` to see the detailed list of your friends.
* Run `vk friends -o` to see the detailed list of your online friends.
* Run `vk friend {id/abbr}` to see detailed information about a contact.
* Run `vk recv {id/abbr}` to receive the unread messages from a contact.
* Run `vk recv -q {id/abbr}` to receive the unread messages from a contact in the stealths mode (the messages will stay unread).
* Run `vk chat {id/abbr}` to enter the chat mode with a contact.

## Feedback
Bugreports should be created [here](https://github.com/caphindsight/VkCli/issues).

