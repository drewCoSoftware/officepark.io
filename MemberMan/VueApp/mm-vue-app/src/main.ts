import './assets/main.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'

const app = createApp(App)


// Thanks Internet!
// https://stackoverflow.com/questions/55905055/vue-need-to-disable-all-inputs-on-page
// This is how we are disabling all of the inputs on a form when it is working.
// It may make more sense to just shove all of this into the EZFORM component....
// TODO: Put this into some kind of reusable file/package....
app.directive("disable-inputs", {
  // When all the children of the parent component have been updated
  updated: function (el, binding) {

    // NOTE: This seems to fire a lot....
    const flag = binding.value;

    // NOTE:  If this is EZForm specific
    const tags = ["input", "button", "textarea", "select"];
    tags.forEach(tagName => {
      const nodes = el.getElementsByTagName(tagName);
      for (let i = 0; i < nodes.length; i++) {
        nodes[i].disabled = flag;
      }
    });
  }
});

// OBSOLETE: We can remove this!
app.directive("enable-submit", {
  updated: function (el, binding) {
    const flag = binding.value;

    const selector = 'button[data-is-submit="true"],button[type="submit"]';
    const nodes = (el.querySelectorAll(selector));
    for (let i = 0; i < nodes.length; i++) {
      const node = nodes[i];
      node.disabled = !flag;
    }

  }
});

// gobals?
// NOTE: Use environment variable here?
app.provide('isTestMode', import.meta.env.VITE_IS_TEST_MODE == 'true');
//app.config.globalProperties.$isTestMode = true;

app.use(createPinia())
app.use(router)

app.mount('#app')
