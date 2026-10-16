using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Dilim SkillMotor prose iskeleti (sıkıştırılmış); sayılar v4 scale ile bağlanır.</summary>
    static class SliceGrammarMotorScaffold
    {
        const string GzipBase64 =
            "H4sIAKCR0moC/81dX4/juJH/KoQPi8xgPI7/d/cc9oDJ7CZ72MslwGQPt1kMBFqmbZ5kyaGk7rUHDdxT8h4cLm83QF72dedln/rp3PNF9pNcVZGiKInu9vzp" +
            "7guSbFsii2T9isVfFSnu6062zXKx7jx73TkXKpNp0nnWOR8/zWIZiqdLxddrrjqX3U6YrmdpUJUOC6VEkgdr/n0Qi2SZrzrPht3OosgLJcyTrPPsu1F3/MrW" +
            "zlUR4nsUMICGvt7/FLGFlHGn2xnC71/Dn+wJe3n9dsFzeDYKtDx49QjfPdFvHrtlxofLPNJ/lk9wGFkk4zgLNkIFs0LG886z0bTbKRL5p0IE+mXn2WA87nby" +
            "NOexLqRfBBsuFbwdjYZTeL+J+RakrLlMbMXO5OkUuhQLrhKZLANQ0jkNNhbnIg5wyMNfxJKlSbylcvh0Ak9Hv4j3V6xI4jSM2CM9oMe2xKAPRcbeIjCkueIX" +
            "2NhGiXMpLrA5kfBZLGBwoHABUuS5CIrNnOeifDQXWajkJteA/25bJGHB9j/InVCRSBgNiMlsLdm5YGIholwynsTXbyO23P8ERqBkLMkwLlSKIyWAlciKOAeB" +
            "C64iKGwUF4odD+bi+g0BveEqAwuBYUDB6x/jiCVplHP2OUuZ2l8l8F+yCbbdX+32V//IZCSTUHpKZYSrKdfDzqgiEWh1rzsSRj/odhK+Rst4yeP59Vt1/Rba" +
            "BzufBQseNp/z+X+IMEdVmZffptdvQN/83Zscyqx5rQxoeyG/t6VKuUlaoEL/rVDFuzfwcMHXErB+1gHbl5GAJyGgsEwVPlvxDCYXmBpXSwFTKZ1jsyIR621g" +
            "TGTGQVdisYBW4dUfRMRWYg54sDlXXZbOOaqZnZfNbXiWYe/mheKILWh9wQmSyWVXK2VolXL941bGAkYn1Vo0FNN811LOl+vGc6uQL9cylA19vPsfueCuOtYi" +
            "mdeVARaZi6iljUzEiyBVAY89+njBE7BPJdWNQz8phz6yQ/+KKxGJvDHo6mlruOBC9j+ANzxgBOb1ljcN4TmY17o28JTmXMMOynZ9Y/cZwtdpUqwZTSnCSIIx" +
            "zHm2Os4Exs68OC8SGlR9VpRP23rgM5nH4hD29Lqhgq95HJFerA6WBVcN9DPb5LEq+AonD4OewNC5WqfqfSxhajUAmsxVGjc0UD1taeBX/PpNfNAQ9Nvt9duw" +
            "8jVGD/jK1UJo2zhCDzc4hRcvWLFdFjFo45FK0/yXWZxe/DLLi+TxjcoYl8o4s8p4Dp4wmZfOzlVI/U1LKb9DR3RQKb/Tbqo5NVDkuuYVNgWM+ePdwhdiViwW" +
            "LAMPpm6ZEa9Mn1CAYSWvOzw06+IchkQdIvnm17Nx3zzYpBJrDSbmd5hmIHQwtD/TeJ5egKB+77TbWcl8lqI6NiolFcUCl6xhrcWV4HHZHv39bDRpNN+vtzZu" +
            "tTbo9Z3WSFMcho6NjRrDI4+B/wjmMst5EooA2N2o17+5zb5nhGdOmyQxlgkNcFxrM1tJEePk138EfJalagZQ3NLk0DPMYWOY2Nq01loYojWFATAIMPQOTg/9" +
            "wNpCJkIU1VTy6e1Knjita+PE9s/q7QMVTDIyaf0XSCiSnKjJ+ynYA2pp/peX7qyDCZJZS475TMR+KqNbDtY0EQa9kR1OkIU8Ll/0exN4QYYeNGoMS+MtGzGU" +
            "IJYLnLNou/3eqNEQ2MmktMOynru2zkA9YIRanxmxFfOoIcbf3UFvUFqcle6sWN5xTHSXc7kWAZ8jQ0JVhzxDZzMDmptvjekAoy4trJTurgabTbwNsJxjVPoh" +
            "emTSRmkgZXXHb8plkkLkREuZZen1nqKjwXjLamdwaUMalDrbBujMLPrlG0OHO4Ong4719dokmGWq1oL85lI5V4d4dpF7RxmyUkNlja/FUIhlCZulGFlwQ1Bx" +
            "iXry2WBiS0OUiE7iXl1u924nBeBxwTdBiL401jgCRtA+lF2LcMUTGd6uRSq+ENqAhcq7LOKKKyoMy6GOhbbcFj2XWYETjiIEDOOWsstmYst3TO465UIPBjCs" +
            "DIB4uh9/M5Mt5lgI+vvZqM9gYNAAjxton/jRrpV+WLSP9U7ocNQcI3vNNHIJfw/7HR+KB/Xi4vd8/0MXeKoOihpwfY2Gd/12BzGc3CHICePR9dt3b4AtOaiN" +
            "KtSqSMMPnetMLX7DXhk2fjbpM7A0cdRcLQs+LHCffHlowXhAPS6I+7+IcxkhQhC/RW0gX3JgswygO79+GwP3X6OYtcikC+PYgRFXpQMIOguWRfD3qFC2/xss" +
            "Kofn3RYCoGQuoJPgKFQG9LrLCAnsGDg4W/+BAf2kK/KRDrehwNoEvYbpBtM4okyQDtwa8H4rlMDpHa05OOC05lKnFapu9OcH1+ELFlvoEqJoyGmF7dg/K8lU" +
            "Jdvyc44PcO16cEjfnwa1AGpowQXIROPh/qqNzB8xNwjgzaAH4ENr8+2sQsaEoH5QHBZmQfkjqHXFtmmUga89ylvusAY04dR5WFA+mlx6UGqrxQXqCxHDMgcz" +
            "SQD1TNpg/Zafmzxul81FDMhcviqjiCZZHXrIaplGPJarPtcZ69n+ChkWrtFtIOGnXDxjPjFt9O4yPO/eebTWQtOvn5pvTADO5f5q/0Psm33findvZAw6zMnQ" +
            "7NwbtommD7smz/xCiYSBETdTeeVCZ7Aqq3lml8LtmPtByUsoJ/54t83/WyOtUf79T+v9FRS5iTH+/Oe/brX6NWV0te8njD4IvHzxC9AKP8QTDQi1ig81U+6e" +
            "GzZU4aL0qwKn6EHGQNAQC3SRaXFALyg+CggcBHdkZMTg/xacQayR6Ja9k6Qm48Hw+dTJlxY+h7VSYxDIiEOMnjM+gxkU3wDZCvcrXMgOETwfcj5+p01oRm8S" +
            "zdYaJM9A5lZ+KMTek8khrAo3ytMNdjFNo87hWVRXQW0q4RuCB91eFMuD6KAMFxwPx/Ph4qN4XyZAohVaBgDKZkVcHPZ2bv2HguYu+NwBHdQoAKwuMYGT2n0c" +
            "PzYI/qsyu9vkcyMPnyt3R4/lc7D4ZUAdgWXvEOj6jucHEbp72AJ5AF53QE8tYhdhuTacv2olDkdtPufDrsXnoGH2xObGPoTQ3Sc+d5Ei9KigxvRkksnj0oMu" +
            "Gn5+54PEy+/2f5GL/Obpcwu9u1dc7p7l1RXiS/zJCBBoAaTrUaLdhadF8rzI+EieMRft2D+Q2t0vNp84mUcjNwVxohWLRWOzlobhsg4Y3wrqwH+VAKjjm+ah" +
            "VayL8W/2V+ci8ZEOSlhE+gCLg+8hRuiD2csIdWf8Gb+jyOC9YvwJsnuNEb9fdq+e1xv5OJ9P8z7OZ/oRod3FItFm+wGM7171fxfEz6uIGiz05tCcoCS0Zntj" +
            "H9sbe9iePQj2XlvNZfLJ4xPfn+7d6emTByB6BzRUYxh68h7AcY4bCvWAd9zmel7kmmSPDuT9/Oe/NrOsxxG9e0LmPc6otJTdGKCr5C9v429QbQ36drXs53Be" +
            "Vd9Vku6elP6gWbrf861O0629eTqaBY0s3bhN4Pyw+BjcNztwdR/F3+4LlTvPzdVV4cvHHWJcxWxWw+MQ4fLCcoc5uHuC5lMQruMybgcAqGfZxj7G5VX9gTTb" +
            "/ofUHG4yiaYoVR+cbLsnDO4o23ZYFb6c2+3sa+pjX1MP+zLHz4/lXmY7Ho+RfyTnuuczuA/Awhq68hzeuynP5qFg0zYF88DXJGAvXnxcru3BgPoYUtYadCu7" +
            "5plDqUfjfjrmUfstJ+w+ipA9GASflqIdeSbLo7Sj91cRwwZtm7Zpmw8+H2k7dATrPVjbw0F35zzuhqNZSPFu3FQlppewHSVyXKwOUToPZD5CpzOvL158IJF7" +
            "MLQ+AbVzh+5icf3jDTnqNgAeTufRvY/ROZ+hFkjxKYn0gWzuwYC4C353UDFHJdac3R6H4J35CN6ZbzPVfk93NMdjc/ul3MeSvPv40OkhqF1DRY0tVJaLtdzF" +
            "vhPhmtwtsI/VrDvz7KN6YWvtpFIvPji7ds/wfAyha460dr50/1OyvwLQ91dqf+XdMSC126NxruYP7Jl61X9XCbd7xuH/7/E4PTsaBO7Ms3Hqh+fm83F4Bow9" +
            "ejIEXB5/MJ27Z6Tu8aBcUz3HJeY0Yo2w6ezwVqgXuDtMzd0zYA+ZotNQ1HN0Z95dUS8IB5J0nD6DgVrgZMMP3Re9ZxTuKEnXVsVxyTkzR0ryhl/nXgi+SZOK" +
            "ulXXr3zLcST5doM/FE+W+mOmcAUqwCt05E5GMOxOa908NfOefEH58KT0G+WDaVcLNQ+GwIhgCS59BR5dqkAxSgOFK8HDFW5kD/t4FgNouFjSC0AiyFacOsuV" +
            "ArOmU1CUkOUbMGBarbU9Bbni5wL5qoahBLUEAsCaiyTHblQ29nX67k3B8HMmXDRYpGQOvuqzIX6uFabrDQQDM1iN8ANn1OagO+qedM9eQS8TuaZIIQsigU5w" +
            "y82VEAjn646JLM7TOBb4ojbE5pBkkgt1zmM9k8vFMKNOKz6XRYY1+70BscWy82YORmlWBNAVyjmt03MRXEDb6YVxCzAOGFSAhoaXZvR7Q/uVDzzHB31kRJQ6" +
            "maVJkVnBO6lWwTZFlulKHPqs/rJ9ywsEFjEygcrc1gKGsj7G3AZ2mamZ21nd3Cjn45obVasbW/8GY+uNHWuj67fq9hZW1gaKSeYi0CB/hL39RiTy3RsGchg0" +
            "CDRfCZrqB2xt4rc1jPBCj7VlsT4gUh9ifUBNWxuelsY2qhvbxGNrS+h+ZrTgas5vQFS6bT1VxdP+Jd6FIpCCJLnvCq3nuaAP58I0Rvg6/7BYjOcDuidmgR4a" +
            "qOJ5EO1/2pllqLr+hWpSwAvrXY696swKldjf1cUtI70z8svMeVnmAfDCGkKAByEoNxNZcNrHAS5VWmxoqHRpHV1Opwt4RvEF4IV2X14UNugOAdxxd0rwuhfx" +
            "kCwEJHNmEHvCwG2zOUkxHh77CWYPOJvF0NwMR88NBLHuFk8CvK3N2in8Rjfh/p6n82XtAaJpU6jmrre6MLxRLZdc3yRUSlzwOLtZpCmRpDkpBgvR1QFyLZnc" +
            "5DxmsI7pu/9g+rdadRrQLdLsngdpEtRZaa0TTi2nF3hzmwhT/Baq2UxTU8CC11IfFbxFXXZoL+ERM/K3+G2iHhVWPWJU1EIwl0qE5uYuGSwgphLZAZWSSRRJ" +
            "gvfv3Si6DdFh7Whef6NmbkfcGBEuQImIbx/7IYnkuFSxIS95WAtzwT+ZCi7R/2eZZ1KJ78FlBnkKkxlJt0phIuJCB65sqfhm1cEkmn3RqFAvVf1qytXU2Rk5" +
            "gEFyDaeuFy9tjYrYH41C1P9mP2F4yyWsRKS3egYUCF6runZAxbZYZ8UuCHHuUsXa5Y4vZcxXrLms0ceHpqaZ9mzOzxVGIkjAeuQkY7oWT6iyBG6y6feqx74x" +
            "tT8Hr72QC4bXPIKDpDgBPxHu2QWaAMPG4oL6dyvBoIfVWu00CzILGZSLkM3ylEpoiffSZdr4b4tf811dfIa3OtD9SnSFJSzmGal0/9+75f5KxfjR7BM9biAY" +
            "S5FJrZW4AKXM+Yrbs3ldq2vQ1v7v8f7vLCpiivsglsG7NFFbsBbLDK/ziwU9o3QYyMVVmfF3b2BgeKvDnAMv7jP8AltHjlgXWOc6IAFrsSsnJYxFAI2G/7kI" +
            "dEwff/7P/2LfkXm80qhJxqNc4ikIq85OiTIW/uy0z0QeSbpJVS54HphycbEsKC0b9dgf9ldrczUnfhaf40+8zzPGBETdgl7yOZAuevs0IzO18q4hGDjnim71" +
            "XMbpDGgSmiNZEgaB5mpRwznpRj808TSTNGVsWWMMO6FSsx+pnQjREMdSABfg3wwvidCv2Tff/uabf3n+r799/kea+OdFnAjFDaOd4f2lWbACMgty9Fvsb8bZ" +
            "CiJanFyoqLoQ/DQsQGeggI9mNhQ20TNaBhSgLrgdQ1Vqw3euwAsWMgZXZDc26KG1sXJXlW5xrXXBkKoEL+PlYURrQOf5FoyoHPYQVsiduVEwgRlR3r+aSf1x" +
            "3layYke3TrBHPN6sOF1Cq0SWFioUzg3BZagBjK2j4276E2J9ov5ASekaXnJup+WTuQDyFPAFjE3TCCejAnADWy5Ac7C8crv0XDqE38JuTMa+MBR7pDsSpkl5" +
            "fzE2osO7G+Q7jI6ko4zVxgylMTKK+7KNACq0Rlo6QVdWkgj3oPhp+bjZyfHQcgxkU7rQGlO20767DmbVRYZVl4bDfr9vPqMwPdKfVKzwzs1ngwn1eClDvKxX" +
            "ZuQhB2UwN+MKP13ESmYlamzDnZAvxCi2wtlGsE5k25+YYmjBchNLNFXKWJL664VPJjZPYmY1LbNlTuk1fV+F2ZVgE4PtvsDBKyKkzXuM93/hasMT8MkwFbc8" +
            "ycFEMWLLtik+1Nmkxz32WxHB86i8QBUWthe/2Ar2CD1tKKLHLKP8WY8yTYC8Bt0ZSoe8VZCDlUDMDw6ZDsKVwSF9k1IGit9R0VffoZN71XFSWrZnPFjS9/7m" +
            "hgW7LDWX7EBgQUWXKmq/p+fcYVkUhy1SBV0H8+y8NuGdCc0u2evmZZ34qOx5gLUvSynZKlW5SAxpNMzg+kdYdNj4CbgMoOWCpXGBHpCOO8W48Sm+5+tNLKog" +
            "0GwnspdyJRX8/AJcSg41f/7zX1lZxDzrmFDWc6H1xUroxBNegRNIICcdm2mDGQ3+VehUW0m+akdRHH9hvMvQAc9G4xNiobRg0O3WNqmoJ225ljjxRnnRKJr2" +
            "sowPcMbYREgYtvnZC/BArCzRZfIpzfwue/FCpx9jCUZqL3KC6Qi0QWnjRJuiq0udPQB9s/pCCaE5tr5Z3f09avweU7ibXqDH0k8mrSfT1pOThpTTxu+zxm+6" +
            "xNx+5jUYtAQOhrUNDM0vrObKZbMc4Ezi0nEutyKIYG0CqmFGWnIoZEF6rE5RCP1mIsvjcti6uWCXKqo/OSR42hJ80npyerips3ZTpI66ANJJ49HwkNDKZdaz" +
            "bKSdAaliSMPv00j7NLinAxoK/uNE/+OUXp1RFezS04Frrriqx4Wx04VMgJ18zqzRgUfVIJXY0AP0i9CTHnsywLvj91cZMECsBrbQZU8H8CcMiHzE9VvQLjwb" +
            "YkFwGEBCPmdaT70qzqYsjvV0OkzsVsGRWRhxUidzaT/IV8EOOJT+as9WHj09aVYd9xtV6QOsTCrgw+DX6/UHg6egpIaEUVPCWibkRzYcPAz4FRLzig61bJRM" +
            "FU1TGBP48EinhUO6IJ5Oj9bX2yE2r8sNTTlzQqbxTWRVblTKAw6qNy8OFh2bolECZHYGdLBVuKPv/O9UdSal+Jjy5fXi46rc1JSbxZJuej/YiRNTEMgRkukb" +
            "Sp6akjnHHaV2Qa3iitcCcLhy6e4idQ2AuCKPnkuISjX7w3cS5zlwqcCQXqC2AdgO2PgOozPwtQFe/VYGoJc1JIkDkFdCAGkX0ICpb+NmSzwf8rijk0JmQ7AE" +
            "mx5pDZk6T1ipML2fqIuToEf4oxKn3xuQTRGafwZ1LF3JxqqVbFOqeq1/OyWsSQTUVcdGsBF8dFk/pbU2PFRfpD3Qn0EuaW9O0/fM7nBrIrhNl3iCh3wdNztY" +
            "W77Vf59e1oJ2Z3GL86AfNFcweBLkKbyYuC+C8y+/fR7oRYYKTbDQIOi3lp5lbh5Xy4/N9GhogXAW+K1xJLIyOGePyBchFmUayLzWvrtK/HhqdRkShy7bAqXS" +
            "cRqFUeb6t9cdyqjofVH80ll/DA20XenvHEAg6AWkLiVl9wG+gFLn7J8MUaD/URb3CRvY0+llvhpodEXgLbkyuTfzG1g4pfUhxLOBo5NTL3P4hlM6OwQm8NMH" +
            "p9o+AgpIsKsy+jUhGU3bzMx3kk8BYa2Bl0Ul3St7ZGTrO1gmdckDKxk0lYua5K+ASd4uW0d1Zmelj6zRL99+oVI18Id0o3h0exPlxyyj/iHh1Q67o/nt/Pqt" +
            "9tS2BZlgsJmTIaKZ14QAdYgayv2aK94QcQDB6iSXEvPCRGl9u7tedfmVmUY6kstutbWZAEtHEzXnj2hjqUmXv6J37N0bvCYIPVa6LdDz/duv/52ZeMxS4zLW" +
            "Ku/DNttvId9kRWwvYczkTuhTzmu2/xvrwz+dMVeLYFeHgFWCe+gKzTZAOpoyh2u8Xgd3jiBKuFEmEJsst1f0l0LNh+CuyJHp5Cl2MlSUDBDl8YsLOc9X9Yb6" +
            "vRGj/bRG58e3dp4UcqD7tJ7Snl5D7ORWsaOjdWKyL9MadmlLI2PSyLT/vz8eJe7kdlPQOh4cqeFD5nF6F+ZxdrvQG2A7KHbQvxtzGNQmHixN5JFqYo/urJVZ" +
            "m3fhFikLHT9sGMWxfS2j9HowU3kNEGg3E8btg8bWEVTliP60tyHq52HtXG/UC1d4h6U5NjW0M7UpvU68RnbqVeWGvfZtlCd2PjXk8fk8qOILO03cUhOnlOaJ" +
            "1sg9wmjpwFXSGq2rRySEQNLj8kCqnZ3GED0ClVjEdEwAUKgsq1EQJipEYkGuJGYvSz0PhkeocHjppCGromQF1OGhPUU1InUMzwgbkjWhMzAnU9IuPTkxl4Gc" +
            "GiDO8J+neoD6SIdZ5ar8XBlhU3ANDkh3pledq8GHNmXXqzpJZzAohcdnmLbTaX1YEYPXX+oF/jJ4jf9yvH+ewx8v0c7hr3o1Y3Anw7owvYU+ZYYokF8c6n8/" +
            "2+fsZMh4wpku2zP/Tj6mQBBmA9cpBFpyK+gcQa8iG+X2mplfEEmtOdHk6kiJhtE+5byPYfcMUF3liciy8l+1AJy0nNeg9HImOhIH/DQkClOT2O/Pz+hpXWK/" +
            "LlGHIaOGxNlITNsSF/SflsRBU+K0nM+OxNPZdDFqjXo8Hy1IF65EOnhVl3hWzvyaHudn4/mRfRw3JVof4UhEXKbTth7xP60+nrSRuazQJ8cdyFDn9jHnBbOV" +
            "ky8oNiaJF0oVxmWu8oJXaco/FVzZDGVO0fzUlUBnR/XcMsc1X9s1Xi8akTBbHnTCLMcbpPq9PioBJgCd4pMZ/buSzCLo1ho2ap1WtShKo62opNHSpFFp2q4U" +
            "y2al4e39swtfrYeN1gZVPfwiqWP3b9zdDFuffPyCjq5h5JfLpLZ0grjLy8v/Aw6SzrY0dAAA";

        internal static JsonValue ParseRoot()
        {
            byte[] gz = System.Convert.FromBase64String(GzipBase64);
            using var ms = new MemoryStream(gz);
            using var gzStream = new GZipStream(ms, CompressionMode.Decompress);
            using var outMs = new MemoryStream();
            gzStream.CopyTo(outMs);
            string json = Encoding.UTF8.GetString(outMs.ToArray());
            return MiniJson.Parse(json);
        }
    }
}
