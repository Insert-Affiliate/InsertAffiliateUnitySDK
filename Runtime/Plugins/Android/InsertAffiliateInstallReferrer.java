package com.insertaffiliate.unity;

import android.content.Context;
import com.unity3d.player.UnityPlayer;
import com.android.installreferrer.api.InstallReferrerClient;
import com.android.installreferrer.api.InstallReferrerStateListener;
import com.android.installreferrer.api.ReferrerDetails;

public class InsertAffiliateInstallReferrer {

    private static final String UNITY_GAME_OBJECT = "InsertAffiliateCoroutineRunner";
    private static final String CALLBACK_METHOD = "OnInstallReferrerReceived";

    public static void getInstallReferrer() {
        Context context = UnityPlayer.currentActivity;
        if (context == null) return;

        InstallReferrerClient client = InstallReferrerClient.newBuilder(context).build();
        client.startConnection(new InstallReferrerStateListener() {
            @Override
            public void onInstallReferrerSetupFinished(int responseCode) {
                if (responseCode == InstallReferrerClient.InstallReferrerResponse.OK) {
                    try {
                        ReferrerDetails details = client.getInstallReferrer();
                        String referrer = details.getInstallReferrer();
                        client.endConnection();
                        UnityPlayer.UnitySendMessage(UNITY_GAME_OBJECT, CALLBACK_METHOD, referrer != null ? referrer : "");
                    } catch (Exception e) {
                        client.endConnection();
                        UnityPlayer.UnitySendMessage(UNITY_GAME_OBJECT, CALLBACK_METHOD, "");
                    }
                } else {
                    client.endConnection();
                    UnityPlayer.UnitySendMessage(UNITY_GAME_OBJECT, CALLBACK_METHOD, "");
                }
            }

            @Override
            public void onInstallReferrerServiceDisconnected() {
                UnityPlayer.UnitySendMessage(UNITY_GAME_OBJECT, CALLBACK_METHOD, "");
            }
        });
    }
}
