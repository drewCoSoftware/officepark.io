<template>
  <SiteHeader v-bind:loginState="loginState" />
  <router-view />
  <button v-on:click="toggleLogin()">Toggle Login</button>
</template>

<script lang="ts">
import SiteHeader from "./components/SiteHeader.vue";
import { Options, Vue } from "vue-class-component";
// import HelloWorld from "./components/HelloWorld.vue";

@Options({
  components: {
    SiteHeader,
  },

  data() {
    return {
      loginState: {
        isLoggedIn: true,
        loginToken: null, // Special token to help with session tracking.
        userID: "drew",
      },
    };
  },
  methods: {
    toggleLogin: function () {
      this.loginState.isLoggedIn = !this.loginState.isLoggedIn;
      if (this.loginState.isLoggedIn) {
        this.$router.push("/sessions");
      }
    },

    // Log out any connected user, and redirect them to the homepage.
    logout: function () {
      if (this.loginState) {
        this.$router.push("/");
      }
    },
  },
  provide() {
    return {
      toggleLogin: this.toggleLogin,
      main_logout: this.logout,
    };
  },
})
export default class App extends Vue {}
</script>

<style lang="less">
#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
  margin-top: 60px;
}
</style>
