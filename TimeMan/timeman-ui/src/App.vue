<template>
  <SiteHeader v-bind:loginState="loginState" />
  <router-view />
  <button v-on:click="toggleLogin()">Toggle Login</button>
  <p>{{ this.$dtAuth.IsLoggedIn }}</p>
</template>

<script lang="ts">
import SiteHeader from "./components/SiteHeader.vue";
import { Options, Vue } from "vue-class-component";
//import { reactive} from 'vue'

@Options({
  components: {
    SiteHeader,
  },

  data() {
    return {
      loginState: this.$dtAuth,
      // isLoggedIn: false,
      // loginToken: null,   // Special token to help with session tracking.
      // userID: "drew",
    };
  },
  methods: {
    toggleLogin: function () {
      // Works great, makes sense to me.
      //this.$dtAuth.IsLoggedIn = !this.$dtAuth.IsLoggedIn;

      // This updates the state of the class just fine, but the UI doesn't get updated.
      // What to do?
      if (!this.$dtAuth.IsLoggedIn) {
        this.$dtAuth.Login();
      } else {
        this.$dtAuth.Logout();
      }

      // This seems like a hack....
      //this.$forceUpdate();
    },

    // Log out any connected user, and redirect them to the homepage.
    logout: function (forcedOut: boolean) {
      if (this.loginState) {
        this.loginState.isLoggedIn = false;

        if (forcedOut) {
          let redirectTo = this.$router.currentRoute.value.path;
          console.log("We will redirect to: " + redirectTo + " on login....");
          this.$router.push("/login?to=" + redirectTo);
        } else {
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
export default class App extends Vue {
  //  private _value : string;
  // public get IsLoggedIn() : boolean {
  //   return false;
  //   //return this._value;
  // }
  // public set value(v : string) {
  //   this._value = v;
  // }
}
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
