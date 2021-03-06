import { createApp } from 'vue'
import App from './App.vue'

import Home from "./components/Home.vue"
import About from "./components/About.vue"
import Sessions from "./components/Sessions.vue"
import NotFound from "./components/NotFound.vue"
import Login from "./components/Login.vue"
import Signup from "./components/Signup.vue"

import { createRouter, createWebHistory } from "vue-router";

import { dtAuth } from "./plugins/dtAuth/dtAuth"

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
    path: "/signup",
    name: "Signup",
    component: Signup,
  },
  {
    path: "/:notfound(.*)",
    name: "NotFound",
    component: NotFound,
  }
];


const authEndpoint = "https://localhost:7138/api/login"
const signupEndpoint = "https://localhost:7138/api/signup"
const auth = new dtAuth(authEndpoint, signupEndpoint);

const router = createRouter({
  history: createWebHistory(),
  routes,
});

router.beforeEach((to, from) => {
  if (to.meta.requiresAuth && !auth.IsLoggedIn) {
    alert('auth required!');
    return {
      path: '/login',
      query: { to: to.fullPath }
    }
  }
});

export default { router, auth };

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


