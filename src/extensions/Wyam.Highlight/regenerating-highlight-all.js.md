Highlight-all.js was created by taking the node version of highlight.js and 
using browserify to make it browser ready by applying a workaround for the require statements.

Even though we aren't using it in a browser, this will get it in a state that we can call it from 
the MsIE JS engine. The browser version had a different issue where it expected a browser to be present
when running. 

This gives us one big file with all the languages that doesn't need npm or a browser.

## To rebuild

This works best from WSL or from any linux distro. Make sure `nodejs` and `npm` are installed.  
Create a folder somewhere and install locally the required node modules:
```
npm install browserify
npm install highlight.js
```
In the same folder create an `index.js` file containing just one line: `hljs = require('highlight.js');`  
Now we can generate the updated highlight-all.js with the following command:
```
$(npm bin)/browserify index.js --standalone hljs -o highlight-all.js
```

It then needs a few tweaks to be compliant with the .net regex engine. 

- for ada replace 
  ```
  var BAD_CHARS = '[]{}%#\'\"'
  ```
  with
  ```
  var BAD_CHARS = '{}%#\'\"'
  ```
- for lisp replace
  ```
  var MEC_RE = '\\|[^]*?\\|';
  ```
  with
  ```
  var MEC_RE = '\\|[\S\s]*?\\|';
  ```

  It can be further minimized using the `uglify.js` library:
  ```
  npm install uglify-js
  $(npm bin)/uglifyjs highlight-all.js --compress -o highlight-all.min.js
  ```