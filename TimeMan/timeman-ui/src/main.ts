import { createApp } from 'vue'
import App from './App.vue'

import Home from "./components/Home.vue"
import About from "./components/About.vue"
import Sessions from "./components/Sessions.vue"
import NotFound from "./components/NotFound.vue"
import Login from "./components/Login.vue"

import { createRouter, createWebHistory } from "vue-router";

import dtAuth from "./plugins/dtAuth"

// https://www.npmjs.com/package/vue3-cookies
import VueCookies from 'vue3-cookies'

const loginStatus = {
  isLoggedIn: false,
  isGobal: true
};

const routes = [
  {
    path: "/",
    name: "Home",
    component: Home,
  },
  {
    path: "/about",
    name: "About",
    component: About,
  },
  {
    path: "/sessions",
    name: "Sessions",
    component: Sessions,
    meta: {
      requiresAuth: true
    }
  },
  {
    path: "/login",
    name: "Login",
    component: Login,
  },
  {
    path: "/:notfound(.*)",
    name: "NotFound",
    component: NotFound,
  }
];

const auth = new dtAuth();

const router = createRouter({
  history: createWebHistory(),
  routes,
});

//export default auth;


router.beforeEach((to, from) => {
  if (to.meta.requiresAuth && !auth.IsLoggedIn) {
    //console.dir(app);
    alert('auth required!');
    return {
      path: '/',
      query: { to: encodeURIComponent(to.fullPath) }
    }
  }
});

export default { router, auth };
//export default dtAuth;

const app = createApp(App);
app.use(router);
app.use(auth);

app.use(VueCookies, {
  expireTimes: "30d",
  path: "/",
  domain: "",
  secure: true,
  sameSite: "None"
});
app.mount('#app')


