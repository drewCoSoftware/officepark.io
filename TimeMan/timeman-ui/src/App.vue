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
        // NOTE: This is where we would grab some kind of redirect url....
        this.$router.push("/sessions");
      }
    },

    // Log out any connected user, and redirect them to the homepage.
    logout: function (forcedOut:boolean) {
      if (this.loginState) {
        this.loginState.isLoggedIn = false;

        if (forcedOut)
        {
          let redirectTo = this.$router.currentRoute.value.path;
          console.log("We will redirect to: " + redirectTo + " on login....");
          this.$router.push("/login?to=" + redirectTo);
        }
        else
        {
          this.$router.push("/");
        }
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
